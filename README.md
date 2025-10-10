# Natural Language Processing

This is a collection of projects implemented in C\# and Python for the NYU CSCI
469 Natural Language Processing course and distributed under the MIT license.

## Regular expressions

This is a pair of regular expression programs.

### Usage

```sh
./NaturalLanguageProcessing.DollarRegex test_dollar_phone_corpus.txt > dollar_output.txt
```

```sh
./NaturalLanguageProcessing.PhoneRegex test_dollar_phone_corpus.txt > telephone_output.txt
```

## Viterbi HMM POS tagger

This is a Viterbi hidden Markov model (HMM) part of speech (POS) tagging
program.

### Usage

```sh
cat WSJ_02-21.pos WSJ_24.pos > pos_file.pos
cat WSJ_23.words > words_file.words
./NaturalLanguageProcessing.ViterbiTagger pos_file.pos words_file.words
```

### Implementation

This program is implemented in C\#/.NET. I developed it in five stages:

1. parsing and summarizing the training data;
2. implementing and testing Viterbi's algorithm;
3. introducing smoothing and handling out-of-vocabulary words;
4. classifying unknown words by shape; and
5. classifying unknown words by suffix.

#### Stage 1

First, I implemented a parser for the training (*.pos) file. This algorithm
required careful bookkeeping for the sentence start (`Begin_Sent`) and end
(`End_Sent`) markers.

I keep counters for instances of words, tags, word-tag pairs (emissions), and
source-target-tag pairs (transitions). In the postprocessing step, the
emission count for each word-tag pair is divided by the total count for its tag,
which gives the Bayesian probability of a word given its tag. Similarly, the
transition count for each source-target-tag pair is divided by the total count
for the source tag, giving the Bayesian probability of a tag given the previous
tag.

I used generic `Dictionary` data structures for the emission (`likelihood`) and
transition (`transition`) tables. This hash-based data structure implements
contains-key, get-value-by-key, and set-value-by-key in constant time and allows
iteration over all key-value pairs in linear time. Tuples are used as composite
keys like word-tag pairs and source-target-tag pairs.

#### Stage 2

In the second stage, I implemented Viterbi's algorithm. First, the `n` POS tags
are numbered as states `0` to `n+1`, where state `0` is the sentence start
marker, state `n+1` is the sentence end marker, and states `1` to `n` are the
POS tags. This allows states to be indexed by number. Numbering states also
allows each state to be used as an array index. Thus, I was able to use
two-dimensional arrays (such matrices are built-in natively in C\#) for the
Viterbi and backpointer tables. This approach (integer keys in a square matrix)
provided a significant performance improvement over string keys in an
array-backed list of dictionaries.

#### Stage 3

In the third stage, I sought to increase the accuracy of the tagger by handling
out-of-vocabulary words. I introduced Laplace smoothing by adding a constant
$k$ to the numerator of each Bayesian probability calculation and $k\times N$,
where $N$ was the number of tags (or words), to the denominator.

Then, I select all words with only a single instance in the training corpus and
recategorized them as unknown words. All unknown words are grouped into a single
token `Unknown_Word`, and the emission and transition probability tables are
updated to match.

In the tagging step, if a word is not in the vocabulary, it is replaced with the
as `Unknown_Word` token.

#### Stage 4

Next, I introduced more advanced classification for unknown words. A set of
bitflags identifies the kind of word based on its "shape." For example, the
presence of an uppercase letter, lowercase letter, digit, hyphen, etc. sets a
bit flag. The resulting integer is appended to the `Unknown_Word` designation
to give separate classes for each combination of unique attributes.

To support these changes, I modified the training corpus parser to convert
the initial character of every sentence to lowercase. This means that a sentence
like "Apples are delicious" becomes "apples are delicious" This transformation
avoids false positives when tagging proper nouns, but does encourage some false
negatives. It is a decent compromise, however, since acronyms are still handled
correctly; for example, the sentence "NASA is amazing" becomes "nASA is
amazing." Since "nASA" contains uppercase letters, the tagger classifies it
alongside other unknown initialisms.

#### Stage 5

Finally, I added morphological classification of unknown words using suffixes.
I initialize the suffix set with a collection of English suffixes. Then, when
encountering an unknown word, I begin with the length of the largest suffix and
search down until reaching the shortest suffix: For each possible suffix length
$k$, the last $k$-letters of the word are compared against the suffix set. If
there is a match, the tagger classifies it alongside other unknown words with
the same suffix. The suffix classification is combined with the "shape"
attributes to produce many combinations of unknown word classes.

## Information retrieval

This is a TF-IDF information retrieval system.

### Usage

```sh
./NaturalLanguageProcessing.InformationRetrieval cran.all.1400 cran.qry
```

### Implementation

This program is implemented in C\#/.NET.

#### Tokenization

Documents and queries are tokenized using a simple regular expression
(`[A-Za-z]+`).

#### Stemming

English stemming is provided by the `Porter2Stemmer` (1.0.0) library.

## License

This repository is licensed with the [MIT](LICENSE.txt) license.
