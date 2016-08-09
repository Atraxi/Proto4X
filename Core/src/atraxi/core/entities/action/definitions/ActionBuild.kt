package atraxi.core.entities.action.definitions

import atraxi.core.Player
import atraxi.core.entities.Entity

/**
 * Created by Atraxi on 2/05/2016.
 */
abstract class ActionBuild protected constructor(source: Entity, player: Player) : Action(source, player)
