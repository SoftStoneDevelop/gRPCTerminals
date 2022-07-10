docker build --rm -t grpc_server -f "gRPCServer.Dockerfile" .
docker create --name server -p 6025:6025 grpc_server