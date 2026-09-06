# dprojects.libs

Repository for libs used in DProjects 

dotnet restore DProjects.Libs.sln
dotnet build DProjects.Libs.sln --configuration Release --no-restore
dotnet test --solution DProjects.Libs.sln --configuration Release --no-build -- --filter-not-trait "Category=Integration" --ignore-exit-code 8
dotnet pack DProjects.Libs.sln --configuration Release --no-build --output artifacts/packages