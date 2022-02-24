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

using Amazon.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace S3Append.Models
{
    /// <summary>
    /// A (composite) response from S3 object append operation.
    /// TODO: Think of how individual responses could be correlated with respective operations
    /// </summary>
    public class AppendObjectResponse : AmazonWebServiceResponse
    {
        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="responses">Responses encountered during append operation</param>
        public AppendObjectResponse(params AmazonWebServiceResponse[] responses)
        {
            ResponseHistory = responses[..^1].ToList();
            ResponseMetadata = responses[^1].ResponseMetadata;
            ContentLength = responses[^1].ContentLength;
            HttpStatusCode = responses[^1].HttpStatusCode;
        }

        /// <summary>
        /// Collection of responses encountered during append operation.
        /// </summary>
        public List<AmazonWebServiceResponse> ResponseHistory { get; }

    }
}
