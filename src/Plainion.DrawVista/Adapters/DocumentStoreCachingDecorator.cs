using Plainion.DrawVista.UseCases;

namespace Plainion.DrawVista.Adapters;

public class DocumentStoreCachingDecorator : IDocumentStore
{
    private IDocumentStore myStore;
    private readonly Dictionary<string, ProcessedDocument> myCache;

    public event EventHandler DocumentsChanged;

    public DocumentStoreCachingDecorator(IDocumentStore store)
    {
        myStore = store;
        myStore.DocumentsChanged += (sender, eventArgs) => DocumentsChanged?.Invoke(sender, eventArgs);

        myCache = myStore
        .GetPageNames()
        .Select(myStore.GetPage)
        .ToDictionary(x => x.Name);
    }

    public void Clear()
    {
        myCache.Clear();
        myStore.Clear();
    }

    public ProcessedDocument GetPage(string pageName) =>
        myCache[pageName];

    public IReadOnlyCollection<string> GetPageNames() =>
        myCache.Keys.ToList();

    public void Save(ProcessedDocument document)
    {
        myStore.Save(document);
        myCache[document.Name] = document;
    }
}
