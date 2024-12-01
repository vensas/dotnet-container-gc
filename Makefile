.PHONY : restart serve test_1 test_10 test_100 test_1000

build:
	eval $(minikube -p minikube docker-env) && \
  	cd DotnetContainerGc && \
	rm -rf publish && \
	dotnet clean && \
	dotnet publish DotnetContainerGc.csproj -c Release -o ./publish -f net8.0 && \
	docker build -t dotnet-container-gc:dotnet8 -f Dockerfile-dotnet8 . && \
	rm -rf publish && \
	dotnet clean && \
	dotnet publish DotnetContainerGc.csproj -c Release -o ./publish -f net9.0 && \
	docker build -t dotnet-container-gc:dotnet9 -f Dockerfile-dotnet9 .

deploy_dotnet8:
	kubectl apply -k kubectl/application/overlays/dotnet8 

deploy_dotnet9:
	kubectl apply -k kubectl/application/overlays/dotnet9

undeploy:
	kubectl delete deployment dotnet-gc-app --ignore-not-found=true

serve:
	 minikube service dotnet-gc-service --url

serve_dotnet8: undeploy deploy_dotnet8 serve

serve_dotnet9: undeploy deploy_dotnet9 serve
