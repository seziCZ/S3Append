/*
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Tests.Unit")]
namespace S3Append.Models
{
    /// <summary>
    /// A request to be used when appending data to S3 bucket hosted object.
    /// </summary>
    public class AppendObjectRequest : PutObjectRequest
    {

        /// <summary>
        /// The requested size of object chunks (parts) to be copied during data append operation.
        /// The smaller the size the better the performance - but also the higher the price.
        /// If not set, value defaults to <see cref="S3ClientExtensions.PART_MAX_BYTES"/>
        /// See https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html
        /// </summary>
        public long? PartMaxBytes { get; set; }

        /// <summary>
        /// The MD5 of the customer encryption key specified in the CopySourceServerSideEncryptionCustomerProvidedKey
        //  property. The MD5 is base 64 encoded. This field is optional, the SDK will calculate
        //  the MD5 if this is not set.
        /// </summary>
        public string CopySourceServerSideEncryptionCustomerProvidedKeyMD5 { get; set; }

        /// <summary>
        /// The customer provided encryption key for the source object of the copy.
        //  Important: Amazon S3 does not store the encryption key you provide.
        /// </summary>
        public string CopySourceServerSideEncryptionCustomerProvidedKey { get; set; }

        /// <summary>
        /// The Server-side encryption algorithm to be used with the customer provided key.
        /// </summary>
        public ServerSideEncryptionCustomerMethod CopySourceServerSideEncryptionCustomerMethod { get; set; }

        /// <summary>
        ///  Specifies a particular version of the source object to copy. By default the latest
        //   version is copied.
        /// </summary>
        public string SourceVersionId { get; set; }

        /// <summary>
        /// Composes <see cref="GetObjectRequest"/> based on user provided preference.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <returns><see cref="GetObjectRequest"/> reflecting user provided preferences</returns>
        internal GetObjectRequest ToGetObjectRequest() =>
            new()
            {
                BucketName = BucketName,
                ExpectedBucketOwner = ExpectedBucketOwner,
                Key = Key,
                RequestPayer = RequestPayer,
                ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod,
                ServerSideEncryptionCustomerProvidedKey = ServerSideEncryptionCustomerProvidedKey,
                ServerSideEncryptionCustomerProvidedKeyMD5 = ServerSideEncryptionCustomerProvidedKeyMD5,
                VersionId = SourceVersionId
            };

        /// <summary>
        /// Enriches provided data (originating from S3 bucket hosted object) with data to be 
        /// appended (originating from this request) and composes <see cref="PutObjectRequest"/>
        /// that could be used to override original (S3 bucket hosted) object.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <param name="existingData">Data associated with existing (S3 bucket hosted) object</param>
        /// <returns><see cref="PutObjectRequest"/> containing both existing and to-be-appended data</returns>
        internal PutObjectRequest ToPutObjectRequest(Stream existingData)
        {
            var compositeStream = new MemoryStream();
            if (existingData.CanSeek)
                existingData.Seek(0, SeekOrigin.Begin);
            existingData.CopyTo(compositeStream);

            if (InputStream is null && ContentBody is not null)
            {
                var contentBytes = Encoding.UTF8.GetBytes(ContentBody);
                compositeStream.Write(contentBytes);
                ContentBody = null;
            }
            else if (InputStream is null && FilePath is not null)
            {
                using var contentStream = File.OpenRead(FilePath);
                contentStream.CopyTo(compositeStream);
                FilePath = null;
            }
            else if (InputStream is not null)
            {
                if (AutoResetStreamPosition && InputStream.CanSeek)
                    InputStream.Seek(0, SeekOrigin.Begin);
                InputStream.CopyTo(compositeStream);
            }

            InputStream = compositeStream;
            AutoResetStreamPosition = true;
            return this;
        }

        /// <summary>
        /// Composes <see cref="InitiateMultipartUploadRequest"/> based on user provided preference.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <returns><see cref="InitiateMultipartUploadRequest"/> reflecting user provided preferences</returns>
        internal InitiateMultipartUploadRequest ToMultiPartInitRequest() =>
            new()
            {
                BucketKeyEnabled = BucketKeyEnabled,
                BucketName = BucketName,
                CannedACL = CannedACL,
                ContentType = ContentType,
                ExpectedBucketOwner = ExpectedBucketOwner,
                Grants = Grants,
                Key = Key,
                RequestPayer = RequestPayer,
                ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod,
                ServerSideEncryptionCustomerProvidedKey = ServerSideEncryptionCustomerProvidedKey,
                ServerSideEncryptionCustomerProvidedKeyMD5 = ServerSideEncryptionCustomerProvidedKeyMD5,
                ServerSideEncryptionKeyManagementServiceEncryptionContext = ServerSideEncryptionKeyManagementServiceEncryptionContext,
                ServerSideEncryptionKeyManagementServiceKeyId = ServerSideEncryptionKeyManagementServiceKeyId,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod,
                StorageClass = StorageClass,
                TagSet = TagSet,
                WebsiteRedirectLocation = WebsiteRedirectLocation
            };

        /// <summary>
        /// Composes <see cref="CompleteMultipartUploadRequest"/> based on user provided preference.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <param name="uploadId">Idenfier associated with multipart upload/copy operation to be completed</param>
        /// <param name="tags">Collection of <see cref="PartETag"/>s to be associated with the request</param>
        /// <returns><see cref="CompleteMultipartUploadRequest"/> reflecting user provided preferences</returns>
        internal CompleteMultipartUploadRequest ToMultiPartCompleteRequest(string uploadId, Dictionary<int, string> tags)
        {
            var eTags = tags.Select(t => new PartETag(t.Key, t.Value));
            return new CompleteMultipartUploadRequest
            {
                BucketName = BucketName,
                ExpectedBucketOwner = ExpectedBucketOwner,
                Key = Key,
                PartETags = eTags.ToList(),
                RequestPayer = RequestPayer,
                UploadId = uploadId
            };
        }

        /// <summary>
        /// Composes <see cref="AbortMultipartUploadRequest"/> based on user provided preference.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <param name="uploadId">Idenfier associated with multipart upload/copy operation to be aborted</param>
        /// <returns><see cref="AbortMultipartUploadRequest"/> reflecting user provided preferences</returns>
        internal AbortMultipartUploadRequest ToMultiPartAbortRequest(string uploadId) =>
            new()
            {
                BucketName = BucketName,
                ExpectedBucketOwner = ExpectedBucketOwner,
                Key = Key,
                RequestPayer = RequestPayer,
                UploadId = uploadId
            };

        /// <summary>
        /// Composes <see cref="CopyPartRequest"/> based on user provided preference.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <param name="uploadId">Idenfier associated with relevant multipart copy operation</param>
        /// <param name="partNumber">Part number to be associated with resulting copy request</param>
        /// <param name="fromBytes">Lower boundary of byte range interval to be copied, inclusive</param>
        /// <param name="toBytes">Upper boundary of byte range interval to be copied, inclusive</param>
        /// <returns><see cref="CopyPartRequest"/> reflecting user provided preferences</returns>
        internal CopyPartRequest ToCopyPartRequest(string uploadId, int partNumber, long fromBytes, long toBytes) =>
            new()
            {
                CopySourceServerSideEncryptionCustomerMethod = CopySourceServerSideEncryptionCustomerMethod,
                CopySourceServerSideEncryptionCustomerProvidedKey = CopySourceServerSideEncryptionCustomerProvidedKey,
                CopySourceServerSideEncryptionCustomerProvidedKeyMD5 = CopySourceServerSideEncryptionCustomerProvidedKeyMD5,
                DestinationBucket = BucketName,
                DestinationKey = Key,
                ExpectedBucketOwner = ExpectedBucketOwner,
                ExpectedSourceBucketOwner = ExpectedBucketOwner,
                FirstByte = fromBytes,
                LastByte = toBytes,
                PartNumber = partNumber,
                ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod,
                ServerSideEncryptionCustomerProvidedKey = ServerSideEncryptionCustomerProvidedKey,
                ServerSideEncryptionCustomerProvidedKeyMD5 = ServerSideEncryptionCustomerProvidedKeyMD5,
                SourceBucket = BucketName,
                SourceKey = Key,
                SourceVersionId = SourceVersionId,
                UploadId = uploadId
            };

        /// <summary>
        /// Composes <see cref="UploadPartRequest"/> based on user provided preference.
        /// TODO: Do not compose request manually, think of futerproof solution
        /// </summary>
        /// <param name="uploadId">Idenfier associated with relevant multipart upload operation</param>
        /// <param name="partNumber">Part number to be associated with resulting upload request</param>
        /// <returns><see cref="UploadPartRequest"/> reflecting user provided preferences</returns>
        internal UploadPartRequest ToUploadPartRequest(string uploadId, int partNumber)
        {
            if (InputStream is null && FilePath is null && ContentBody is not null)
            {
                var contentBytes = Encoding.UTF8.GetBytes(ContentBody);
                InputStream = new MemoryStream(contentBytes);
                ContentBody = null;
            }

            return new UploadPartRequest
            {
                BucketName = BucketName,
                CalculateContentMD5Header = CalculateContentMD5Header,
                DisableMD5Stream = DisableMD5Stream,
                DisablePayloadSigning = DisablePayloadSigning,
                ExpectedBucketOwner = ExpectedBucketOwner,
                FilePath = FilePath,
                FilePosition = 0,
                InputStream = InputStream,
                Key = Key,
                MD5Digest = MD5Digest,
                PartNumber = partNumber,
                RequestPayer = RequestPayer,
                ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod,
                ServerSideEncryptionCustomerProvidedKey = ServerSideEncryptionCustomerProvidedKey,
                ServerSideEncryptionCustomerProvidedKeyMD5 = ServerSideEncryptionCustomerProvidedKeyMD5,
                UseChunkEncoding = UseChunkEncoding,
                UploadId = uploadId
            };
        }
    }
}
