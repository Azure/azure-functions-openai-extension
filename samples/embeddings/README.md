## Introduction

## Embeddings Generator
Processing of the source text files typically involves chunking the text into smaller pieces, such as sentences or paragraphs, and then making an OpenAI call to produce embeddings for each chunk independently. Finally, the embeddings need to be stored in a database or other data store for later use.

#### [C# embeddings generator example](./samples/embeddings/csharp-ooproc/EmbeddingsGenerator.cs)

```csharp
[Function(nameof(GenerateEmbeddings_Http_RequestAsync))]
public async Task GenerateEmbeddings_Http_RequestAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] HttpRequestData req,
    [EmbeddingsInput("{RawText}", InputType.RawText)] EmbeddingsContext embeddings)
{
    using StreamReader reader = new(req.Body);
    string request = await reader.ReadToEndAsync();

    EmbeddingsRequest? requestBody = JsonSerializer.Deserialize<EmbeddingsRequest>(request);

    this.logger.LogInformation(
        "Received {count} embedding(s) for input text containing {length} characters.",
        embeddings.Count,
        requestBody.RawText.Length);

    // TODO: Store the embeddings into a database or other storage.
}
```

#### [Python example](./samples/embeddings/python/)

```python
@app.function_name("GenerateEmbeddingsHttpRequest")
@app.route(route="embeddings", methods=["POST"])
@app.generic_input_binding(arg_name="embeddings", type="embeddings", data_type=func.DataType.STRING, input="{rawText}", input_type="rawText", model="%EMBEDDING_MODEL_DEPLOYMENT_NAME%")
def generate_embeddings_http_request(req: func.HttpRequest, embeddings: str) -> func.HttpResponse:
    user_message = req.get_json()
    embeddings_json = json.loads(embeddings)
    embeddings_request = {
        "raw_text": user_message.get("RawText"),
        "file_path": user_message.get("FilePath")
    }
    logging.info(f'Received {embeddings_json.get("count")} embedding(s) for input text '
        f'containing {len(embeddings_request.get("raw_text"))} characters.')
    # TODO: Store the embeddings into a database or other storage.
    return func.HttpResponse(status_code=202)
```

## Prerequisites

The sample is available in the following language stacks:

* [C# on the out-of-process worker](csharp-ooproc)
* [Python](python)
* [Powershell](powershell)
* [Java](java)
* [NodeJS](nodejs)

Please refer to the [root README](../../README.md#requirements) for common prerequisites that apply to all samples.


## Running the sample

1. Clone this repo and navigate to the sample folder.
1. Use a terminal window to navigate to the sample directory (e.g. `cd samples/embeddings/csharp-ooproc/Embeddings`)
2. If using python, run `pip install -r requirements.txt` to install the correct library version.
2. Run `func start` to build and run the sample function app

    If successful, you should see the following output from the `func` command:

    ```plaintext
    Functions:

        GenerateEmbeddings_Http_RequestAsync: [POST] http://localhost:7071/api/embeddings

        GetEmbeddings_Http_FilePath: [POST] http://localhost:7071/api/embeddings-from-file
    ```

1. Use an HTTP client tool to send a `POST` request to the `GenerateEmbeddings_Http_RequestAsync` function. The following is an example request:

    ```http
    POST http://localhost:7071/api/embeddings
    ```

    > **NOTE:** All the HTTP requests in this sample can also be found in the [demo.http](demo.http) file, which can be opened and run in most IDEs.

    You should see some relevant log output in the terminal window where the app is running.


1. Use an HTTP client tool to send a `POST` request to the `GetEmbeddings_Http_FilePath` function. The following is an example request:

    ```http
    POST http://localhost:7071/api/embeddings-from-file
    ```

    > **NOTE:** All the HTTP requests in this sample can also be found in the [demo.http](demo.http) file, which can be opened and run in most IDEs.

    You should see some relevant log output in the terminal window where the app is running.
