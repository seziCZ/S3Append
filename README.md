# .NET AWS SDK S3 Append 

[![Project license](https://img.shields.io/github/license/sezicz/s3append.svg?style=flat-square)](LICENSE)
[![Pull Requests welcome](https://img.shields.io/badge/PRs-welcome-ff69b4.svg?style=flat-square)](https://github.com/sezicz/s3append/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)
[![Code with love by sezicz](https://img.shields.io/badge/%3C%2F%3E%20with%20%E2%99%A5%20by-sezicz-ff1414.svg?style=flat-square)](https://github.com/sezicz)


## About

Project offers `AppendObjectAsync` extension method for [IAmazonS3](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/index.html?page=TIS3.html&tocid=Amazon_S3_IAmazonS3) .NET client capable of performant data appends to [AWS Simple Storage Service](https://aws.amazon.com/s3/) (S3) hosted objects. Implementation utilizes [S3 multipart upload/copy](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html) internally, allowing for "mostly server side" handling. For more information regarding the "mostly" part of the story, please relate to [multipart upload limits](https://docs.aws.amazon.com/AmazonS3/latest/userguide/qfacts.html) documentation.

## Usage

Assuming that `109a6d191b67` S3 bucket contains `fa5ec9042bc3` object with plain text content `Hello `, following (pseudo) code

```
using Amazon.S3;
using S3Append.Extensions;
using S3Append.Models;

var client = new AmazonS3Client();
var request = new AppendObjectRequest
{
	BucketName = "109a6d191b67",
	Key = "fa5ec9042bc3",
	ContentBody = "world!"
};

await client.AppendObjectAsync(request);
```

would result in (the same) `fa5ec9042bc3` object containing `Hello world!`.