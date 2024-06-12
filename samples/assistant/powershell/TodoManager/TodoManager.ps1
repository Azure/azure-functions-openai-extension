function Add-Todo {
    param($id, $task)

    $ErrorActionPreference = "Stop"
    
    if (-not $task) {
        throw "Task description cannot be empty"
    }
    Write-Information "Adding todo: $task"
    
    $manager = Get-TodoManager
    $manager.Add($id, $task)
}

function Get-Todos {
    $ErrorActionPreference = "Stop"
    Write-Information "Fetching list of todos"

    $manager = Get-TodoManager
    return $manager.Get()
}

function Get-TodoManager {
    $ErrorActionPreference = "Stop"

    if (-not $env:CosmosDbConnectionString) {
        if (-not $memoryTodoManager) {
            $memoryTodoManager = New-Object MemoryTodoManager
        }
        return $memoryTodoManager
    }
    else {
        if (-not $cosmosTodoManger) {
            $cosmosTodoManger = New-Object CosmosDbTodoManager
        }
        return $cosmosTodoManger
    }
}

$memoryTodoManager = $null
$cosmosTodoManger = $null

class ITodoManager {
    [void] Add($Id, $Task) {}
    [string[]] Get() { throw "Not implemented" }
}

class MemoryTodoManager : ITodoManager {
    [string[]] $todos = @()

    [void] Add($Id, $Task) {
        $document = @"
{
    "id": "$Id",
    "task": "$Task"
}
"@
        $this.todos += $document
    }

    [string[]] Get() {
        return $this.todos | Foreach-Object { $_ | Out-String }
    }
}

class CosmosDbTodoManager : ITodoManager {
    $cosmosDbConnectionString = $null
    $cosmosDbDatabaseName = $null  
    $cosmosDbCollectionName = $null
    $cosmosDbContext = $null
    
    CosmosDbTodoManager() {
        $this.cosmosDbConnectionString = $env:CosmosDbConnectionString
        $this.cosmosDbDatabaseName = $env:CosmosDatabaseName
        $this.cosmosDbCollectionName = $env:CosmosContainerName

        if (-not $this.cosmosDbDatabaseName -or -not $this.cosmosDbDatabaseName -or -not $this.cosmosDbCollectionName) {
            throw "CosmosDatabaseName and CosmosContainerName must be set as environment variables or in local.settings.json"
        }
        
        # ToDo: Use managed identity to authenticate with Cosmos DB or key vault to store the connection string
        $connectionStringSecure = $this.cosmosDbConnectionString
        $this.cosmosDbContext = New-CosmosDbContext -ConnectionString $connectionStringSecure 
        
        try {
            $cosmosDatabase = Get-CosmosDbDatabase -Context $this.cosmosDbContext -Id $this.cosmosDbDatabaseName
        }
        catch [System.Management.Automation.MethodInvocationException], [System.Management.Automation.ParentContainsErrorRecordException], [System.Management.Automation.RuntimeException]
        {
            $cosmosDatabase = New-CosmosDbDatabase -Context $this.cosmosDbContext -Id $this.cosmosDbDatabaseName
        }
        try {
            $cosmosContainer = Get-CosmosDbCollection -Context $this.cosmosDbContext -Id $this.cosmosDbCollectionName -Database $this.cosmosDbDatabaseName
        }
        catch [System.Management.Automation.MethodInvocationException], [System.Management.Automation.ParentContainsErrorRecordException], [System.Management.Automation.RuntimeException], [Microsoft.PowerShell.Commands.HttpResponseException] {
            $cosmosContainer = New-CosmosDbCollection -Context $this.cosmosDbContext -Id $this.cosmosDbCollectionName -PartitionKey "/id" -OfferThroughput 2500 -Database $this.cosmosDbDatabaseName
        }
    }

    [void] Add($Id, $Task) {
        $document = @"
{
    "id": "$Id",
    "task": "$Task"
}
"@

        New-CosmosDbDocument -Context $this.cosmosDbContext -Database $this.cosmosDbDatabaseName -CollectionId $this.cosmosDbCollectionName -DocumentBody $document -PartitionKey $Id
    }

    [string[]] Get() {
        Write-Information "Getting all todos from container $this.cosmosDbCollectionName"

        $results = Get-CosmosDbDocument -Context $this.cosmosDbContext -Database $this.cosmosDbDatabaseName -CollectionId $this.cosmosDbCollectionName

        return $results | ForEach-Object { $_ | Out-String }
    }
}