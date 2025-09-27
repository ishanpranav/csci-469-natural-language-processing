# Viterbi HMM POS tagger

This is a Viterbi hidden Markov model (HMM) part of speech (POS) tagging program
implemented in C\# for the NYU CSCI 469 Natural Language Processing course and
distributed under the MIT license.

## Usage

```sh
cat WSJ_02-21.pos WSJ_24.pos > pos_file.pos
cat WSJ_23.words > words_file.words
./NaturalLanguageProcessing.ViterbiTagger pos_file.pos words_file.words > submission.pos
```

## Implementation

I develoepd this program in five stages:

1. parsing and summarizing the training data;
2. implementing and testing Viterbi's algorithm;
3. introducing smoothing and handling out-of-vocabulary words;
4. classifying unknown words by shape; and
5. classifying unknown words by suffix.

### Stage 1

## License

This project is licensed with the [MIT](LICENSE.txt) license.
