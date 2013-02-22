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

#ifndef _HYDRAPLAYER_H_
#define _HYDRAPLAYER_H_

#include "T3D/player.h"
#include "ts/tsShapeInstance.h"

//----------------------------------------------------------------------------

struct HydraPlayerData: public PlayerData {
   typedef PlayerData Parent;

   S32 wristRNode;   // Right wrist node
   S32 wristLNode;   // Left wrist node

   F32 shoulderWidth;   // Width of the shoulders (left to right shoulder nodes)
   F32 armLength;       // Length of the arms (shoulder to wrist)

   DECLARE_CONOBJECT(HydraPlayerData);
   HydraPlayerData();

   static void initPersistFields();

   bool preload(bool server, String &errorStr);
};

//----------------------------------------------------------------------------

class HydraPlayer: public Player
{
   typedef Player Parent;

protected:
   enum MaskBits {
      HydraNodesUpdateMask = Parent::NextFreeMask << 0,
      NextFreeMask         = Parent::NextFreeMask << 1
   };

   enum Constants
   {
      HydraNodes = 2,   // Needs to be at least two for the wrists
   };

   // Client interpolation data for Hydra controlled nodes
   struct NodeStateDelta {
      Point3F pos[HydraNodes];
      VectorF posVec[HydraNodes];
      QuatF   rot[HydraNodes];
      QuatF   rotPrev[HydraNodes];
      F32     dt;
   };
   NodeStateDelta mNodeDelta;

   // Callback for when the shape's nodes are about to be recalculated.
   // Called during TSShapeInstance::animateNodes().
   class TSNodeCallback : public TSCallback
   {
      public:
         HydraPlayer* mPlayer;
         S32 mWristNodes[2];
      public:
         TSNodeCallback();
         virtual ~TSNodeCallback() { mPlayer=NULL; }
         virtual void setNodeTransform(TSShapeInstance * si, S32 nodeIndex, MatrixF & localTransform);
   };
   TSNodeCallback mNodeCallback;

   HydraPlayerData* mDataBlock;

   // Hydra controlled nodes
   Point3F  mWristPos[2];
   QuatF    mWristRot[2];

   virtual void updateMove(const Move *move);

   void setWristValues(const Point3F& pos0, const QuatF& rot0, const Point3F& pos1, const QuatF& rot1);

public:
   DECLARE_CONOBJECT(HydraPlayer);

   HydraPlayer();
   virtual ~HydraPlayer();

   static void consoleInit();
   static void initPersistFields();

   //
   bool onAdd();
   void onRemove();
   bool onNewDataBlock( GameBaseData *dptr, bool reload );

   virtual void interpolateTick(F32 dt);
   virtual void advanceTime(F32 dt);

   //
   void writePacketData(GameConnection *con, BitStream *stream);
   void readPacketData(GameConnection *con, BitStream *stream);
   U32 packUpdate(NetConnection *con, U32 mask, BitStream *stream);
   void unpackUpdate(NetConnection *con, BitStream *stream);

   virtual void getMuzzleVector(U32 imageSlot,VectorF* vec);

   const Point3F& getWristPos(U32 index) const;
   const QuatF& getWristRot(U32 index) const;
};

inline const Point3F& HydraPlayer::getWristPos(U32 index) const
{
   return (index >= 2) ? Point3F::Zero : mWristPos[index];
}

inline const QuatF& HydraPlayer::getWristRot(U32 index) const
{
   return (index >= 2) ? QuatF::Identity : mWristRot[index];
}

#endif   // _HYDRAPLAYER_H_
