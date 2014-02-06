Github-Acceptance-Hook
==========
This repo contains code for an API with a /hooks route, meant to be used as a callback for Github issue comment events.

It's made to look for a configurable keyword for a configurable set of users, and automatically create a pull request and merge it for the related branch. 

The purpose is to allow product reps control over 'acceptance' of features without needing the code themselves.
