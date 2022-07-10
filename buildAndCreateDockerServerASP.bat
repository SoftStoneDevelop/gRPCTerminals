docker build --rm -t grpc_server_asp -f "gRPCServerASP.Dockerfile" .
docker create --name server_asp -p 6024:6024 grpc_server_asp