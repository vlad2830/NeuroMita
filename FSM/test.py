import asyncio

from Events.PlayerEvents import PlayerEvents
from FiniteStateMachine import FiniteStateMachine
from MitaStates import MitaDefaultState

async def main():
    fsm = FiniteStateMachine(MitaDefaultState())
    await fsm.handle_event(PlayerEvents.TOUCH_LAPTOP) # MitaDefaultState => MitaMurderState
    await fsm.handle_event(PlayerEvents.FIND_BUTTON) # MitaMurderState => ничего не происходит
if __name__ == "__main__":
    asyncio.run(main())
