// Tag.cs
// Copyright (c) 2025 Ishan Pranav
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace NaturalLanguageProcessing.ViterbiTagger;

internal sealed class Tag : IEquatable<Tag>
{
    private readonly Dictionary<string, int> _emissions = new Dictionary<string, int>();
    private readonly Dictionary<Tag, int> _transitions = new Dictionary<Tag, int>();

    public Tag(string value)
    {
        Value = value;
    }

    public string Value { get; }
    public int TotalEmissions { get; private set; }
    public int TotalTransitions { get; private set; }

    public IReadOnlyDictionary<string, int> Emissions
    {
        get
        {
            return _emissions;
        }
    }

    public IReadOnlyDictionary<Tag, int> Transitions
    {
        get
        {
            return _transitions;
        }
    }

    public void AddEmission(string word)
    {
        TotalEmissions++;

        if (_emissions.TryGetValue(word, out int count))
        {
            _emissions[word] = count + 1;
        }
        else
        {
            _emissions[word] = 1;
        }
    }

    public void AddTransition(Tag next)
    {
        TotalTransitions++;

        if (_transitions.TryGetValue(next, out int count))
        {
            _transitions[next] = count + 1;
        }
        else
        {
            _transitions[next] = 1;
        }
    }

    public bool Equals(Tag? other)
    {
        return other != null && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Tag tag && Equals(tag);   
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }
}
