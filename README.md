# Neutrino

## Full Text Match Pattern Format

Escape Character => \

Wildcard (cannot be in a segment) => `*`

Wildcard with Length (can be in a segment) => `<n>`

Text => `'This is a string'`

Negation (can be used on text) => `!`

Spaces, newlines etc are ignored outside of text.

Example:
```
* 'I need to find this text' <11> `but there are 11 chars in between'<5>!'and it cannot end with this.'
```