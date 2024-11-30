# Dotnet Containerization and Garbage Collection 

ℹ️ This example was put together on a Mac using shell scripts and [homebrew](https://brew.sh). 

## Motivation



## Setup

### Run a local kubernetes cluster

```sh
brew install minikube
minikube start
```

### Build the image

```sh
eval $(minikube -p minikube docker-env) # Configure the shell to use minikube docker environment
cd DotnetContainerGc
dotnet clean
dotnet publish DotnetContainerGc.csproj -c Release -o ./publish
docker build -t dotnet-container-gc:dotnet9 -f Dockerfile-dotnet9 .
cd ..
```

Change `TargetFramework` in `./DotnetContainerGc/DotnetContainerGc.csproj` manually and build again with tag `dotnet8`.

### Verify image is available
```sh
minikube image ls --format table
```

### Setup test environment
```sh
kubectl apply -f ./kubectl/observability
kubectl apply -f ./kubectl/application
```

### Expose service through minikube

```sh
minikube service dotnet-gc-service --url
```

### Test service 

```sh
curl <url>/people
# Should print an empty array
```

## Run Tests

### Install k6
```sh
brew install k6
```

### Run k6 tests

```sh
minikube service dotnet-gc-service --url
# Copy URL
k6 run k6/test.js -e BASE_URL=<url> -e MAX_USERS=100
```

### Access Grafana
```sh
minikube service grafana
```

Login using default user `admin` with password `admin`. 
Import Dashboard JSON from `./grafana/dotnet-opentelemetry-dashboard.json`.

## Teardown

### Teardown test environment
```sh
kubectl delete -f ./kubectl
```

### Reset shell configuration for minikube
```sh
 eval $(minikube docker-env --unset)
```