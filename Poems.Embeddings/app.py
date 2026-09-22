import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel, Field
from starlette.concurrency import run_in_threadpool

from embeddings import DIMENSIONS, MODEL, REVISION, VERSION, EmbeddingEngine


@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.engine = await run_in_threadpool(EmbeddingEngine)
    await run_in_threadpool(app.state.engine.embed_query, "Поиск русской поэзии")
    app.state.inference = asyncio.Lock()
    yield


app = FastAPI(lifespan=lifespan, docs_url=None, redoc_url=None)


class QueryRequest(BaseModel):
    query: str = Field(max_length=1000)


class PoemRequest(BaseModel):
    author: str
    title: str
    content: str


async def infer(request: Request, function, *args):
    # A single in-flight inference bounds memory and CPU use. Waiting requests can cancel.
    try:
        await asyncio.wait_for(app.state.inference.acquire(), timeout=5)
    except TimeoutError:
        raise HTTPException(503, "Сервис поиска занят. Попробуйте ещё раз.")
    try:
        if await request.is_disconnected():
            raise HTTPException(499, "Request cancelled")
        return await run_in_threadpool(function, *args)
    finally:
        app.state.inference.release()


@app.get("/health")
async def health():
    return {"version": VERSION, "model": MODEL, "revision": REVISION, "dimensions": DIMENSIONS}


@app.post("/embed/query")
async def embed_query(body: QueryRequest, request: Request):
    try:
        vector = await infer(request, app.state.engine.embed_query, body.query.strip())
    except ValueError as error:
        raise HTTPException(400, str(error))
    return {"version": VERSION, "embedding": vector}


@app.post("/embed/poem")
async def embed_poem(body: PoemRequest, request: Request):
    chunks = await infer(request, app.state.engine.embed_poem, body.author, body.title, body.content)
    return {"version": VERSION, "chunks": chunks}
