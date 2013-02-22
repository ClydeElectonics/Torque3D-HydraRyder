//-----------------------------------------------------------------------------
// Copyright (c) 2012 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// DeathmatchGame
// ----------------------------------------------------------------------------
// Depends on methods found in gameCore.cs.  Those added here are specific to
// this game type and/or over-ride the "default" game functionaliy.
//
// The desired Game Type must be added to each mission's LevelInfo object.
//   - gameType = "Deathmatch";
// If this information is missing then the GameCore will default to Deathmatch.
// ----------------------------------------------------------------------------

// States for the game:
$GameStateWaitForPlayer = 0;
$GameStateReset = 1;
$GameStateCalibration = 2;
$GameStateCalibrationStage1 = 3;
$GameStateCalibrationStage2 = 4;
$GameStateCalibrationComplete = 5;
$GameStateReady = 6;
$GameStatePlay = 7;
$GameStateEnd = 8;

$GameDisplayInfoTextTicks = 900;

function HydraGame::onMissionLoaded(%game)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::onMissionLoaded");

   $Server::MissionType = "Hydra";
   parent::onMissionLoaded(%game);
}

function HydraGame::initGameVars(%game)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::initGameVars");

   //-----------------------------------------------------------------------------
   // What kind of "player" is spawned is either controlled directly by the
   // SpawnSphere or it defaults back to the values set here. This also controls
   // which SimGroups to attempt to select the spawn sphere's from by walking down
   // the list of SpawnGroups till it finds a valid spawn object.
   // These override the values set in core/scripts/server/spawn.cs
   //-----------------------------------------------------------------------------
   
   // Leave $Game::defaultPlayerClass and $Game::defaultPlayerDataBlock as empty strings ("")
   // to spawn a the $Game::defaultCameraClass as the control object.
   $Game::defaultPlayerClass = "HydraPlayer";
   $Game::defaultPlayerDataBlock = "DefaultPlayerData";
   $Game::defaultPlayerSpawnGroups = "PlayerSpawnPoints PlayerDropPoints";

   //-----------------------------------------------------------------------------
   // What kind of "camera" is spawned is either controlled directly by the
   // SpawnSphere or it defaults back to the values set here. This also controls
   // which SimGroups to attempt to select the spawn sphere's from by walking down
   // the list of SpawnGroups till it finds a valid spawn object.
   // These override the values set in core/scripts/server/spawn.cs
   //-----------------------------------------------------------------------------
   $Game::defaultCameraClass = "Camera";
   $Game::defaultCameraDataBlock = "Observer";
   $Game::defaultCameraSpawnGroups = "CameraSpawnPoints PlayerSpawnPoints PlayerDropPoints";

   // Set the gameplay parameters
   %game.duration = 0;
   %game.endgameScore = 0;
   %game.endgamePause = 0;
   %game.allowCycling = false;   // Is mission cycling allowed?

   %game.currentGameState = $GameStateWaitForPlayer;
   %game.previousGameState = %game.currentGameState;
}

function HydraGame::startGame(%game)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::startGame");

   parent::startGame(%game);

   // Create the ScriptTickObject for the game's state machine
   new ScriptTickObject(GameStateTicker);
   MissionCleanup.add(GameStateTicker);
}

function HydraGame::endGame(%game)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::endGame");

   parent::endGame(%game);
}

function HydraGame::onGameDurationEnd(%game)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::onGameDurationEnd");

   parent::onGameDurationEnd(%game);
}

function HydraGame::onMissionLoaded(%game)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::onMissionLoaded");

   parent::onMissionLoaded(%game);
}

function HydraGame::onClientEnterGame(%game, %client)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::onClientEnterGame");

   parent::onClientEnterGame(%game, %client);
   
   // Indicate that the player is now in the game
   %game.currentGameState = $GameStateReset;
}

function HydraGame::onClientLeaveGame(%game, %client)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::onClientLeaveGame");

   parent::onClientLeaveGame(%game, %client);
   
   // Indicate that the player is has left the game
   %game.currentGameState = $GameStateEnd;
}

function HydraGame::loadOut(%game, %player)
{
   //echo (%game @ "\c4 -> " @ %game.class @ " -> HydraGame::loadOut");

   %player.clearWeaponCycle();
   
   %player.setInventory(Ryder, 1);
   %player.setInventory(RyderClip, %player.maxInventory(RyderClip));
   %player.setInventory(RyderAmmo, %player.maxInventory(RyderAmmo));    // Start the gun loaded
   %player.addToWeaponCycle(Ryder);
   
   %player.mountImage(Ryder.image, 0);
   %player.mountImage(RyderWeaponLeftImage, 1);
}

function Game::calibrationStageHit(%game)
{
   %currentState = Game.currentGameState;
   
   if(%currentState == $GameStateCalibrationStage1)
   {
      //echo("@@@ $GameStateCalibrationStage1 calculation");
      
      // This is the arms down stage.  Get the frame data for both controllers.
      %frame = RazerHydraFrameGroup.getObject(0);
      
      // Store the data
      Game.calibrationLeft = %frame.getControllerPos(0);
      Game.calibrationRight = %frame.getControllerPos(1);
      
      //echo("@@@ Calibration Left : " @ Game.calibrationLeft @ "  Right: " @ Game.calibrationRight);
      
      %game.currentGameState = $GameStateCalibrationComplete;
   }
   
   Game.previousGameState = %currentState;
}

function Game::calculateControllerOffsets(%game)
{
   %armLength = DefaultPlayerData.getArmLength();
   
   // Left controller
   Game.LeftOffsetX = Game.calibrationLeft.x;
   Game.LeftOffsetY = Game.calibrationLeft.y;
   
   // Right controller
   Game.RightOffsetX = Game.calibrationRight.x;
   Game.RightOffsetY = Game.calibrationRight.y;
   
   // Both controllers
   Game.LeftOffsetZ = (Game.calibrationLeft.z + Game.calibrationRight.z) * 0.5;
   Game.RightOffsetZ = Game.LeftOffsetZ;
   
   // Scale the width to the player skeleton
   %shoulderWidth = DefaultPlayerData.getShoulderWidth();
   %width = (Game.RightOffsetX - Game.LeftOffsetX) * 0.001;
   %scale = %shoulderWidth / %width;
   Game.controllerScale = %scale;
   //echo("@@@ Controller scale: " @ %scale);
}

function Game::resetCanTargets(%game)
{
   physicsRestoreState();
}

// ----------------------------------------------------------------------------
// GameStateTicker
// ----------------------------------------------------------------------------

function GameStateTicker::onProcessTick(%this)
{
   %currentState = Game.currentGameState;
   
   switch(%currentState)
   {
      case $GameStateReset:
         Game.LeftOffsetX = 0;
         Game.LeftOffsetY = 0;
         Game.LeftOffsetZ = 0;
         Game.RightOffsetX = 0;
         Game.RightOffsetY = 0;
         Game.RightOffsetZ = 0;
         
         // Switch to the Calibration state
         Game.currentGameState = $GameStateCalibration;
         
      case $GameStateCalibration:
         //echo("@@@ $GameStateCalibration");
         
         // Set up the controls for calibration
         %this.activateCalibrationControls(true);
         
         // If the play GUI is already up then display our calibration GUI.
         // Otherwise, this will be taken care of when the client is ready.
         if(Canvas.getContent() == PlayGui.getId())
         {
            Canvas.pushDialog(CalibrationDialog);
         }
         
         // Move on to the first stage of calibration
         Game.currentGameState = $GameStateCalibrationStage1;
         
      case $GameStateCalibrationComplete:
         //echo("@@@ $GameStateCalibrationComplete");
         
         // Deactivate calibration controls
         %this.activateCalibrationControls(false);
         
         // Calculate the controller offsets based on the calibration data
         Game.calculateControllerOffsets();
         
         Canvas.popDialog(CalibrationDialog);

         // Switch to the Ready state
         Game.currentGameState = $GameStateReady;
         
      case $GameStateReady:
         // Set up the controls for the player
         %this.activateGamePlayControls(true);
         
         // Info GUI
         StartInfoText.setVisible(true);
         Game.infoGuiCountdown = $GameDisplayInfoTextTicks;
         
         // Stop the music
         schedule(5000, 0, "sfxStopAndDelete", MainMenuGui.music);
         
         // Switch to the Play state
         Game.currentGameState = $GameStatePlay;
         
      case $GameStatePlay:
         // Game play code could go here
         
         // Handle the info GUI
         if(Game.infoGuiCountdown > 0)
         {
            Game.infoGuiCountdown--;
            if(Game.infoGuiCountdown == 0)
            {
               StartInfoText.setVisible(false);
            }
         }
      
      case $GameStateEnd:
         %this.activateGamePlayControls(false);
         
         // Switch to the Wait state
         Game.currentGameState = $GameStateWaitForPlayer;
   }
   
   Game.previousGameState = %currentState;
}

function GameStateTicker::onRemove(%this)
{
   // We may not make it to the $GameStateEnd tick in onProcessTick()
   // so handle the state change here when the mission ends.
   %currentState = Game.currentGameState;
   if(%currentState == $GameStatePlay)
   {
      %this.activateGamePlayControls(false);
      Game.currentGameState = $GameStateWaitForPlayer;
      Game.previousGameState = %currentState;
   }
}

function GameStateTicker::activateGamePlayControls(%this, %state)
{
   if(%state == true)
   {
      $RazerHydra::SeparatePositionEvents = false;
      $RazerHydra::CombinedPositionEvents = true;
      gamePlayMap.push();
   }
   else
   {
      $RazerHydra::SeparatePositionEvents = false;
      $RazerHydra::CombinedPositionEvents = false;
      gamePlayMap.pop();
   }
}

function GameStateTicker::activateCalibrationControls(%this, %state)
{
   if(%state == true)
   {
      $RazerHydra::GenerateWholeFrameEvents = true;
      $RazerHydra::CombinedPositionEvents = true;
      calibrationMap.push();
   }
   else
   {
      $RazerHydra::GenerateWholeFrameEvents = false;
      $RazerHydra::CombinedPositionEvents = false;
      calibrationMap.pop();
   }
}

function calibrationComplete()
{
   if(Game.currentGameState == $GameStateCalibrationStage2)
   {
      Game.currentGameState = $GameStateCalibrationComplete;
      $calibrationFireTriggered = false;
   }
}

//----------------------------------------------------------------------------
// Client
//----------------------------------------------------------------------------

function GameConnection::initialControlSet(%this)
{
   echo ("*** Initial Control Object");

   // The first control object has been set by the server
   // and we are now ready to go.
   
   if (Canvas.getContent() != PlayGui.getId())
   {
      Canvas.setContent(PlayGui);
      if(Game.currentGameState == $GameStateCalibration || Game.currentGameState == $GameStateCalibrationStage1)
      {
         Canvas.pushDialog(CalibrationDialog);
      }
   }
}
