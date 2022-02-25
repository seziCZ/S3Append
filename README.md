# .NET AWS SDK S3 Append 

[![Project license](https://img.shields.io/github/license/sezicz/s3append.svg?style=flat-square)](LICENSE)
[![Pull Requests welcome](https://img.shields.io/badge/PRs-welcome-ff69b4.svg?style=flat-square)](https://github.com/sezicz/s3append/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)
[![Code with love by sezicz](https://img.shields.io/badge/%3C%2F%3E%20with%20%E2%99%A5%20by-sezicz-ff1414.svg?style=flat-square)](https://github.com/sezicz)


## About
`S3 Append` provides [AppendObjectAsync](./S3Append/Extensions/S3ClientExtensions.cs) extention method for [.NET AWS SDK S3 client](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/index.html?page=TIS3.html&tocid=Amazon_S3_IAmazonS3), capable of appending data to existing S3 hosted objects.

## Performance
Implemented functionality relies on "server side" [AWS S3 multipart copy](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html) operation, making its performance superior to naive download-update-upload approach. In a nutshel, existing portion of data is being [copied](https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPartCopy.html) by S3 internally while only to-be-appended chunk is [uploaded](https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html) from client's side. For details (and limitations) please refer to [relevant documentation](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html).

## Implications
The key consideration is costs. Compared to naive approach, at least five AWS S3 requests have to be issued for every and each append operation:

* [Yield object metadata](https://docs.aws.amazon.com/AmazonS3/latest/API/API_HeadObject.html) to determine its size 
* [Create multipart upload](https://docs.aws.amazon.com/AmazonS3/latest/API/API_CreateMultipartUpload.html) through which data is to be copied/uploaded
* [Copy existing data](https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPartCopy.html) on the server side
* [Upload new data](https://docs.aws.amazon.com/AmazonS3/latest/API/API_UploadPart.html) from client side
* [Complete multipart upload](https://docs.aws.amazon.com/AmazonS3/latest/API/API_CompleteMultipartUpload.html), overriding original object

Moreover, single `UploadPartCopy` operation could only copy up to 5 GiB of data. That said, append to an object with size of 5 TB would result in (at least) 1004 requests issued by `S3 Append` logic.

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