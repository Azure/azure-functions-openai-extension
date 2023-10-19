# Setting up Environment Variables

1. Create a file in this folder called `.env`.  **Security Note** `.env` file must never check in to github repos, and it is excluded in .gitignore.  Add these contents and fill in values.  Choose either the AZURE* values or the OPENAI_API_KEY, but not both.

.env contents (keep secure)
```
OPENAI_API_KEY=""
OPENAI_API_VERSION="2023-05-15"
AZURE_OPENAI_KEY=""
AZURE_OPENAI_ENDPOINT="https://***.openai.azure.com/"
AZURE_OPENAI_CHATGPT_DEPLOYMENT=""
```

2. Export the environment variables from `.env` to your machine

Windows:
```ps
.\dotenvtoenvars.ps1 .\.env -Verbose -RemoveQuotes
```

Mac/Linux:
```sh
set -o allexport; source ./.env; set +o allexport
```
