# Restores a SaaS generated .bacpac file to a Docker container MS SQL instance using sqlpackage
# The backup must be named kentico.community.bacpac and be saved in the .\database folder of this solution

dotnet tool install -g microsoft.sqlpackage

sqlpackage /a:Import `
    /sf:"..\database\kentico.community.bacpac" `
    /tsn:"localhost, 1433 " `
    /tdn:"Kentico.Community.2025-08-24" `
    /tp:"Pass@12345" `
    /tu:"sa" `
    /ttsc:True