# Publishing this repository

This folder is already a git repository with its history. Two ways to get it
onto GitHub, both create the repository as **private**.

## With the GitHub CLI

```bash
gh auth login                 # once, if you have not done it before
gh repo create LivingSoldiers --private --source=. --remote=origin --push
```

Then the release with both packages:

```bash
gh release create v1.19.0 \
  "LivingSoldiers_v1.19.0.zip" \
  "LivingSoldiers_v1.19.0_Server.zip" \
  --title "1.19.0" \
  --notes-file RELEASE_NOTES.md
```

Run it from the game folder, or give the full paths to the two zip files.

## Without the CLI

1. Create a new **private** repository named `LivingSoldiers` on github.com,
   without a README, .gitignore or licence.
2. In this folder:

```bash
git remote add origin https://github.com/<your-name>/LivingSoldiers.git
git push -u origin main
```

3. On the repository page: *Releases* → *Draft a new release* → tag `v1.19.0`,
   title `1.19.0`, paste `RELEASE_NOTES.md` as the description, and drag both
   zip files into the attachment area.

## A word on making it public

The source here was reconstructed from the released DLL of someone else's mod,
and the package contains their character assets. Publishing it would
redistribute their work. If you want the repository public, ask Toni Macaroni
first — the credit in the README is not a substitute for permission.
