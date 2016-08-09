package atraxi.core.entities.action.definitions

import atraxi.core.Player
import atraxi.core.entities.Entity

/**
 * Created by Atraxi on 17/05/2016.
 */
abstract class ActionAttack(source: Entity, player: Player, protected var target: Entity) : Action(source, player)
