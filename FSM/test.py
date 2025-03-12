import asyncio

from Events.PlayerEvents import PlayerEvents
from FiniteStateMachine import FiniteStateMachine
from Crazy.MitaStates import MitaDefaultState

async def main():
    fsm = FiniteStateMachine(MitaDefaultState())
    fsm.handle_event(PlayerEvents.TOUCH_LAPTOP) # MitaDefaultState => MitaMurderState
    fsm.handle_event(PlayerEvents.FIND_BUTTON) # MitaMurderState => ничего не происходит
if __name__ == "__main__":
    asyncio.run(main())
