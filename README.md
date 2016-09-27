# Proto4X
Hobby dev of an RPG set in a 4X strategy game.

## Vision
*(in a perfect world, this is probably not viable without being very simplistic in most areas)*

Complex and rich simulation of 4X style game, where all 'empires' are typically fully AI controlled
- AI is a layered hierarchy where each layer is approximated by npc characters (e.g. ship captain, fleet admiral, planetary governer, president/emperor)
- AI 'difficulty' defined by skills/abilities of character. Characters can change role, causing a new role appropriate AI module to be created based off character abilities
- Characters ideally will have an associated personality (stretch goal/dlc?), such that some characters should be viewed as a valued ally or hated enemy through dynamic player interaction (i.e. Rivalry should grow/be cultivated over time through character interactions, likewise for some allies)

Player is a character like all other npcs, and can switch role
- Switch role is probably time limited, i.e. can only switch role every X years, or on special conditions, to force some investment/attachment to the position and to limit desire to micromanage and control everything when running an entire empire
- Switching role trades scope for direct control (scope being single ship vs planet/system/sector vs empire)
- Emperor cannot command individual ships or manage planets in detail, but can give fleets broad orders (e.g. defend this system or frontier, harass enemy trade(pirate)) and control research and empire level diplomacy
- Sectors/planets also control their own diplomacy/research with options for different interpretation of orders or outright rebellion

## Build/Run Instructions
### Build
- With Intellij: Most settings should already be in the existing project, assuming Intellij can find a valid jdk1.8, git, etc.
- Without Intellij: Untested. There is a dependency on https://mvnrepository.com/artifact/org.json/json/20160212. Both client and server modules depend on core.

### Run
main() is in Proto.java for both the server and client modules. The server *must* be running before starting the client. At startup the client attempts to connect to the server via localhost:6789 (hardcoded currently)
*Run configurations are not currently in the repository. Other problems are known on a fresh machine. More detail coming soon*
