# AWS .NET SDK S3 Append 

[![Project license](https://img.shields.io/github/license/sezicz/s3append.svg?style=flat-square)](LICENSE)
[![Pull Requests welcome](https://img.shields.io/badge/PRs-welcome-ff69b4.svg?style=flat-square)](https://github.com/sezicz/s3append/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)
[![Code with love by sezicz](https://img.shields.io/badge/%3C%2F%3E%20with%20%E2%99%A5%20by-sezicz-ff1414.svg?style=flat-square)](https://github.com/sezicz)


## About
`S3 Append` provides [AppendObjectAsync](./S3Append/Extensions/S3ClientExtensions.cs) extension method for [.NET AWS SDK S3 client](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/index.html?page=TIS3.html&tocid=Amazon_S3_IAmazonS3), capable of appending data to existing S3 hosted objects.

## Implementation
Since AWS S3 is not a [block storage system](https://en.wikipedia.org/wiki/Block-level_storage), content of hosted objects could not be altered in situ. That said, one would have to download, update and upload (override) the object again but such approach suffers of multiple problems (network throughput dependency, high memory requirements, etc.) that makes it practically unusable. For this reason, `S3 Append` implementation relies on [AWS S3 multipart copy](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html) internally to avoid a need for data to be downloaded first:

* [Get to-be-updated object's metadata](https://docs.aws.amazon.com/AmazonS3/latest/API/API_HeadObject.html) to determine its size 
* [Create multipart upload](https://docs.aws.amazon.com/AmazonS3/latest/API/API_CreateMultipartUpload.html) through which data is to be copied/uploaded
* [Copy existing data](https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPartCopy.html) on the server side
* [Upload new data](https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html) from client side
* [Complete multipart upload](https://docs.aws.amazon.com/AmazonS3/latest/API/API_CompleteMultipartUpload.html), overriding original object


## Implications
Thanks to high degree of parallelism and almost unbounded network bandwidth, AWS S3 copy operations are lightning fast (compared to the naive download-update-upload approach). Moreover, internal AWS data transfers are [free of charge](https://aws.amazon.com/s3/pricing/), making the proposed solution a no brainer in situation where client logic resides outside of AWS cloud. Still, cost is the key aspect to be considered as (compared to the naive approach) at least five AWS S3 requests must be issued for every and each `AppendObjectAsync` method call (see [Implementation](#implementation) for details).

> Note that single `UploadPartCopy` operation could only copy up to 5 GiB of data. That said, append to an object with size of 5 TB would result in (at least) 1004 copy requests issued by `S3 Append` logic.


## Usage
When imported into scope, `AppendObjectAsync` could be used in in a straightforward fashion. Consider S3 bucket `109a6d191b67` hosting object `fa5ec9042bc3` with plain text content `Hello`. Following code would, when executed,

```
using Amazon.S3;
using S3Append.Extensions;
using S3Append.Models;
using System.Threading.Tasks;

public static async Task Main(string[] args)
{
	var client = new AmazonS3Client();
	var request = new AppendObjectRequest
	{
		BucketName = "109a6d191b67",
		Key = "fa5ec9042bc3",
		ContentBody = " world!"
	};

	await client.AppendObjectAsync(request);
}
```

result in (the same) `fa5ec9042bc3` object containing proverbial `Hello world!`.
