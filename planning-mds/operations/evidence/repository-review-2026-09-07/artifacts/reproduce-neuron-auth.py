import asyncio, base64, json, sys
from pathlib import Path
from types import SimpleNamespace
sys.path.insert(0,str(Path('neuron').resolve()))
import httpx
from app.main import create_app
from app.persistence.in_memory import InMemoryNeuronRepository
from app.errors import UpstreamAuthError

class RejectingEngine:
    async def call(self, method, path, *, user_token, **kwargs):
        raise UpstreamAuthError(401, 'synthetic engine rejected token')

def forged(sub):
    payload=base64.urlsafe_b64encode(json.dumps({'sub':sub,'exp':1,'iss':'untrusted'}).encode()).decode().rstrip('=')
    return 'eyJhbGciOiJub25lIn0.'+payload+'.invalid-signature'

async def main():
    app=create_app()
    repo=InMemoryNeuronRepository()
    app.state.runtime=SimpleNamespace(
        repository=repo,
        settings=SimpleNamespace(auth_mode='engine'),
        engine_client=RejectingEngine(),
    )
    thread=await repo.create_thread('synthetic-victim',title='Synthetic private conversation')
    await repo.add_message(thread.id,'synthetic-victim',role='user',parts=[('text',{'part_type':'text','text':'synthetic private text'})])
    async with httpx.AsyncClient(transport=httpx.ASGITransport(app=app),base_url='http://validation.local') as c:
        headers={'Authorization':'Bearer '+forged('synthetic-victim')}
        result=await c.get('/v1/threads',headers=headers)
        print('forged expired token list:',result.status_code,result.json())
        result=await c.get(f'/v1/threads/{thread.id}/messages',headers=headers)
        print('forged expired token history:',result.status_code,result.json())
        result=await c.delete(f'/v1/threads/{thread.id}',headers=headers)
        print('forged expired token delete:',result.status_code)
        result=await c.post('/v1/threads',headers={'Authorization':'Bearer '},json={'title':'empty token'})
        print('empty bearer create:',result.status_code)
asyncio.run(main())
