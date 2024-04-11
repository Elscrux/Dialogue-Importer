namespace DialogueImplementationTool.Parser;

public interface IDocumentIterator {
    string FilePath { get; }

    public int Index { get; set; }

    public int LastIndex { get; }

    public void Previous() {
        if (Index > 0) Index--;
    }

    public void Next() {
        if (Index < LastIndex) Index++;
    }

    public void SkipMany();
    public void BacktrackMany();

    public string PreviewCurrent() {
        return Preview(Index);
    }

    public string Preview(int index);
}
