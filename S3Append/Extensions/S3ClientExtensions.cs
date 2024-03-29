﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one or more contributor license agreements.  See the LICENSE file
 * distributed with this work for additional information regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *    
 *    http://www.apache.org/licenses/LICENSE-2.0
 *    
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the License for the 
 * specific language governing permissions and limitations under the License.
 */

using Amazon.S3;
using Amazon.S3.Model;
using S3Append.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Range = System.Tuple<long, long>;

[assembly: InternalsVisibleTo("Tests.Unit")]
namespace S3Append.Extensions
{
    /// <summary>
    /// Enriches <see cref="IAmazonS3"/> client functionality with <see cref="AppendObjectAsync"/> method.
    /// For more information please relate to README.md file hosted by project's repository root folder.
    /// </summary>
    public static class S3ClientExtensions
    {
        // Must be 5 MiB <= PART_MIN_BYTES > PART_MAX_BYTES - PART_MIN_BYTES <= 5 GiB - PART_MIN_BYTES
        // TODO: Make PART_MAX_BYTES user configurable to allow for performance/cost finetuning
        // See https://docs.aws.amazon.com/AmazonS3/latest/userguide/qfacts.html
        internal static readonly long PART_MIN_BYTES = 5 * (long)Math.Pow(2, 20); // aka 5 MiB
        internal static readonly long PART_MAX_BYTES = 5 * (long)Math.Pow(2, 30); // aka 5 GiB

        /// <summary>
        /// Appends data associated with provided <see cref="AppendObjectRequest"/> to referenced, S3 hosted object.
        /// If the is no S3 hosted object to append the data to, new object (containing provided data) is created instead.
        /// Note that method issues multiple S3 requests internally, make sure to understand (cost) implications before use
        /// </summary>
        /// <param name="s3Client">Client that mediates access to AWS SImple Storage Service (S3)</param>
        /// <param name="request">Container for the necessary parameters to execute the AppendObjectAsync service method.</param>
        /// <param name="cancellationToken">Operation cancellation token</param>
        /// <returns>The response from the AppendObjectAsync service method</returns>
        public static async Task<AppendObjectResponse> AppendObjectAsync(this IAmazonS3 s3Client, AppendObjectRequest request, CancellationToken cancellationToken = default)
        {
            InitiateMultipartUploadResponse initMultipartRes = null;
            try
            {
                // ...if existing object is smaller than PART_MIN_BYTES, append "in memory"
                var metadata = await s3Client.GetObjectMetadataAsync(request.BucketName, request.Key, cancellationToken);
                if (metadata.ContentLength < PART_MIN_BYTES)
                {
                    var getReq = request.ToGetObjectRequest();
                    using var getRes = await s3Client.GetObjectAsync(getReq, cancellationToken);

                    var appendReq = request.ToPutObjectRequest(getRes.ResponseStream);
                    var updateRes = await s3Client.PutObjectAsync(appendReq, cancellationToken);
                    return new AppendObjectResponse(metadata, getRes, updateRes);
                }

                // ...otherwise, initiate "server side" multipart copy...
                var initMultipartReq = request.ToMultiPartInitRequest();
                initMultipartRes = await s3Client.InitiateMultipartUploadAsync(initMultipartReq, cancellationToken);

                var copyPartRanges = GetPartRanges(metadata.ContentLength, request.PartMaxBytes ?? PART_MAX_BYTES);
                var uploadPartReq = request.ToUploadPartRequest(initMultipartRes.UploadId, copyPartRanges.Count + 1);
                var uploadPartResTask = s3Client.UploadPartAsync(uploadPartReq, cancellationToken)
                    .ContinueWith(t => t.Result.ETag, TaskContinuationOptions.ExecuteSynchronously);

                var multiResTasks = new List<Task<string>>();
                foreach (var (partId, range) in copyPartRanges.Select((r, i) => (i + 1, r)))
                {
                    var copyPartReq = request.ToCopyPartRequest(initMultipartRes.UploadId, partId, range.Item1, range.Item2);
                    multiResTasks.Add(s3Client.CopyPartAsync(copyPartReq, cancellationToken)
                        .ContinueWith(t => t.Result.ETag, TaskContinuationOptions.ExecuteSynchronously));
                }

                multiResTasks.Add(uploadPartResTask);
                var strTags = await Task.WhenAll(multiResTasks);

                // ...complete multipart copy/upload, have a beer!
                var eTags = Enumerable.Range(0, strTags.Length).ToDictionary(i => i + 1, i => strTags[i]);
                var completeMultipartReq = request.ToMultiPartCompleteRequest(initMultipartRes.UploadId, eTags);
                var completeMultipartRes = await s3Client.CompleteMultipartUploadAsync(completeMultipartReq, cancellationToken);
                return new AppendObjectResponse(metadata, initMultipartRes, completeMultipartRes);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {

                // ...also, consider situation where object does not exist yet
                var response = await s3Client.PutObjectAsync(request, cancellationToken);
                return new AppendObjectResponse(response);
            }
            catch (Exception) when (initMultipartRes is not null)
            {
                // ...abort (clean up) multipart upload in case of a problem
                var abortMultipartReq = request.ToMultiPartAbortRequest(initMultipartRes.UploadId);
                await s3Client.AbortMultipartUploadAsync(abortMultipartReq, CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        /// Divides provided object size into ranges that obey <see cref="PART_MIN_BYTES"/> 
        /// and <see cref="PART_MAX_BYTES"/> S3 multipart upload/copy specific requirements.
        /// See https://docs.aws.amazon.com/AmazonS3/latest/userguide/qfacts.html
        /// </summary>
        /// <param name="totalBytes">Total bytes to be transfered during multi part copy</param>
        /// <param name="partMaxBytes">Maximal allowed size of a single copy part</param>
        /// <param name="enforceSize">Ensures last range is bigger than <see cref="PART_MIN_BYTES"/></param>
        /// <returns>Collection of ranges to be used when issuing multipart upload/copy requests towards S3</returns>
        internal static List<Range> GetPartRanges(long totalBytes, long partMaxBytes, bool enforceSize = true)
        {
            if (totalBytes < PART_MIN_BYTES)
                throw new ArgumentException("Multipart copy could not be used when appending to S3 " +
                    $"object with size smaller than {PART_MIN_BYTES} bytes.", nameof(totalBytes));

            if (partMaxBytes < 2 * PART_MIN_BYTES || partMaxBytes > PART_MAX_BYTES)
                throw new ArgumentException("Requested part size does not fall within allowed " +
                    $"{2 * PART_MIN_BYTES} - {PART_MAX_BYTES} bytes interval.", nameof(partMaxBytes));

            var firstByte = 0L;
            var result = new List<Range>();
            while (firstByte < totalBytes)
            {
                var lastByte =
                    firstByte + partMaxBytes - 1 < totalBytes - 1 ?
                    firstByte + partMaxBytes - 1 :
                    totalBytes - 1;

                result.Add(Tuple.Create(firstByte, lastByte));
                firstByte = lastByte + 1;
            }

            var lastRange = result[^1];
            if (enforceSize && lastRange.Item2 - lastRange.Item1 < PART_MIN_BYTES - 1)
            {
                // To obey "minimal part size" requirement, last "too small" part is being
                // enriched with PART_MIN_BYTES portion of penultimate part's data.
                var newPenultimate = Tuple.Create(result[^2].Item1, result[^2].Item2 - PART_MIN_BYTES);
                var newLast = Tuple.Create(newPenultimate.Item2 + 1, lastRange.Item2);
                result.RemoveRange(result.Count - 2, 2);
                result.Add(newPenultimate);
                result.Add(newLast);
            }

            return result;
        }
    }
}
