using Grpc.Core;
using System;

namespace Server.Helpers
{
    internal static class GrpcHeaderHelper
    {
        public static string GetGuidFromHeaderOrThrowCancellCallException(ServerCallContext context)
        {
            var guidStr = context.RequestHeaders.GetValue("guid");

            if (string.IsNullOrEmpty(guidStr))
            {
                throw new RpcException(new Status(StatusCode.Cancelled, "Header not contain guid"));
            }

            if(!Guid.TryParse(guidStr, out var guid))
            {
                throw new RpcException(new Status(StatusCode.Cancelled, "Guid is not valid"));
            }

            return guid.ToString();
        }
    }
}