using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System;

[Export(typeof(ClassificationTypeDefinition))]
[Name("DesoloKeyword")]
internal static ClassificationTypeDefinition DesoloKeyword = null;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = "DesoloKeyword")]
[Name("DesoloKeyword")]
[UserVisible(true)]
internal class DesoloKeywordFormat : ClassificationFormatDefinition
{
    public DesoloKeywordFormat()
    {
        this.ForegroundColor = System.Windows.Media.Colors.Blue;
        this.IsBold = true;
    }
}

[Export(typeof(IClassifierProvider))]
[ContentType("desolo")]
internal class DesoloClassifierProvider : IClassifierProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationRegistry = null;

    public IClassifier GetClassifier(ITextBuffer buffer)
        => buffer.Properties.GetOrCreateSingletonProperty(
            () => new DesoloClassifier(buffer, ClassificationRegistry));
}

internal class DesoloClassifier : IClassifier
{
    private readonly ITextBuffer _buffer;
    private readonly IClassificationType _keywordType;

    public DesoloClassifier(ITextBuffer buffer, IClassificationTypeRegistryService registry)
    {
        _buffer = buffer;
        _keywordType = registry.GetClassificationType("DesoloKeyword");
    }

    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
        var list = new List<ClassificationSpan>();
        string text = span.GetText();
        int index = text.IndexOf("print");
        if (index >= 0)
        {
            var keywordSpan = new SnapshotSpan(span.Snapshot, span.Start + index, 5);
            list.Add(new ClassificationSpan(keywordSpan, _keywordType));
        }

        return list;
    }
}