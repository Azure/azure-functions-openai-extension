using System.ClientModel.Primitives;
using System.Text.Json;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Embeddings;

class JsonModelListWrapper : IJsonModel<List<string>>
{
    readonly List<string> list;

    public JsonModelListWrapper(List<string> list)
    {
        this.list = list;
    }

    public void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        writer.WriteStartArray();
        foreach (string item in this.list)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }

    public static JsonModelListWrapper FromList(List<string> list)
    {
        return new JsonModelListWrapper(list);
    }

    public List<string> Create(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    public BinaryData Write(ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    public List<string> Create(BinaryData data, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    public string GetFormatFromOptions(ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }
}