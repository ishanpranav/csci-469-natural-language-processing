// TagCollection.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System.Collections.ObjectModel;

namespace NaturalLanguageProcessing.ViterbiTagger;

internal class TagCollection : KeyedCollection<string, Tag>
{
    protected override string GetKeyForItem(Tag item)
    {
        return item.Value;
    }
}
