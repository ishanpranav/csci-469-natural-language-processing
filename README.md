# Viterbi HMM POS tagger

This is a Viterbi hidden Markov model (HMM) part of speech (POS) tagging program
implemented in C\# for the NYU CSCI 469 Natural Language Processing course and
distributed under the MIT license.

## Usage

```sh
cat WSJ_02-21.pos WSJ_24.pos >pos_file.pos
cat WSJ_23.words WSJ_24.words >words_file.words
./NaturalLanguageProcessing.ViterbiTagger words_file.words pos_file.pos > submission.pos
```

## License

This repository is licensed with the [MIT](LICENSE.txt) license.
