package atraxi.core.entities.action.definitions

import atraxi.core.Player
import atraxi.core.entities.Entity

import java.awt.Point

/**
 * Created by Atraxi on 2/05/2016.
 */
abstract class ActionMove(source: Entity, player: Player, protected var target: Point) : Action(source, player)
