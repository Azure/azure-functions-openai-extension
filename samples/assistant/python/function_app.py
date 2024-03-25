import azure.functions as func

from assistant_apis import apis
from assistant_skills import skills

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)

app.register_functions(apis)
app.register_functions(skills)
