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

#include "hydraRyder/hydraPlayer.h"
#include "core/stream/bitStream.h"
#include "math/mathIO.h"
#include "ts/tsShapeInstance.h"
#include "T3D/gameBase/extended/extendedMove.h"
#include "console/consoleTypes.h"
#include "console/engineAPI.h"

//-----------------------------------------------------------------------------

IMPLEMENT_CO_DATABLOCK_V1(HydraPlayerData);

HydraPlayerData::HydraPlayerData() : PlayerData()
{
   wristRNode = -1;
   wristLNode = -1;

   shoulderWidth = 0.0f;
   armLength = 0.0f;
}

void HydraPlayerData::initPersistFields()
{
   Parent::initPersistFields();
}

bool HydraPlayerData::preload(bool server, String &errorStr)
{
   if (!Parent::preload(server, errorStr))
      return false;

   if(mShape)
   {
      // We have mShape at this point.  Resolve nodes.
      wristRNode = mShape->findNode("WristRight");
      wristLNode = mShape->findNode("WristLeft");

      // Calculate the distances for the skeleton
      S32 shoulderLeft = mShape->findNode("ShoulderLeft");
      S32 shoulderRight = mShape->findNode("ShoulderRight");
      if(wristRNode != -1 && wristLNode != -1 && shoulderLeft != -1 && shoulderRight != -1)
      {
         TSShapeInstance* si = new TSShapeInstance(mShape, false);
         TSThread* thread = si->addThread();

         si->setSequence(thread, mShape->findSequence("root"), 0);
         si->animate();

         // Shoulder width
         MatrixF* mat = &(si->mNodeTransforms[shoulderLeft]);
         Point3F slPos = mat->getPosition();
         mat = &(si->mNodeTransforms[shoulderRight]);
         Point3F srPos = mat->getPosition();
         shoulderWidth = (srPos - slPos).len();

         // Length of arm (use right arm)
         mat = &(si->mNodeTransforms[wristRNode]);
         Point3F wrPos = mat->getPosition();
         armLength = (wrPos - srPos).len();

         delete si;
      }
   }

   return true;
}

DefineEngineMethod( HydraPlayerData, getShoulderWidth, F32, ( ),,
   "@brief Provides the width of the shoulders.\n\n"
   "The width is defined as the distance between the left and right should nodes.\n\n"
   "@return The width of the shoulders.\n")
{
   return object->shoulderWidth;
}

DefineEngineMethod( HydraPlayerData, getArmLength, F32, ( ),,
   "@brief Provides the length of the arms.\n\n"
   "The arm length is defined as the distance from the right shoulder node to the "
   "right wrist node.\n\n"
   "@return The length of the arms.\n")
{
   return object->armLength;
}

//-----------------------------------------------------------------------------

IMPLEMENT_CO_NETOBJECT_V1(HydraPlayer);

HydraPlayer::HydraPlayer() : Player()
{
   for(U32 i=0; i<2; ++i)
   {
      mWristPos[i] = Point3F::Zero;
      mWristRot[i] = QuatF::Identity;
   }

   for(U32 i=0; i<HydraNodes; ++i)
   {
      mNodeDelta.pos[i] = Point3F::Zero;
      mNodeDelta.posVec[i] = Point3F::Zero;
      mNodeDelta.rot[i] = QuatF::Identity;
      mNodeDelta.rotPrev[i] = QuatF::Identity;
   }
   mNodeDelta.dt = 1.0f;

   mNodeCallback.mPlayer = this;
}

HydraPlayer::~HydraPlayer()
{
   mNodeCallback.mPlayer = NULL;
}

void HydraPlayer::consoleInit()
{
}

void HydraPlayer::initPersistFields()
{
   Parent::initPersistFields();
}

bool HydraPlayer::onAdd()
{
   if(!Parent::onAdd() || !mDataBlock)
      return false;

   return true;
}

void HydraPlayer::onRemove()
{
   Parent::onRemove();
}

bool HydraPlayer::onNewDataBlock( GameBaseData *dptr, bool reload )
{
   mDataBlock = dynamic_cast<HydraPlayerData*>(dptr);
   if ( !mDataBlock || !Parent::onNewDataBlock( dptr, reload ) )
      return false;

   // Identify the wrist nodes to the node callback class
   mNodeCallback.mWristNodes[0] = mDataBlock->wristRNode;
   mNodeCallback.mWristNodes[1] = mDataBlock->wristLNode;

   // Mark these nodes for control by code only (will not animate in a sequence)
   if (mDataBlock->wristRNode != -1)
   {
      //mShapeInstance->setNodeAnimationState(mDataBlock->wristRNode, TSShapeInstance::MaskNodeHandsOff);
      mShapeInstance->setNodeAnimationState(mDataBlock->wristRNode, TSShapeInstance::MaskNodeCallback, &mNodeCallback);
   }
   if (mDataBlock->wristLNode != -1)
   {
      //mShapeInstance->setNodeAnimationState(mDataBlock->wristLNode, TSShapeInstance::MaskNodeHandsOff);
      mShapeInstance->setNodeAnimationState(mDataBlock->wristLNode, TSShapeInstance::MaskNodeCallback, &mNodeCallback);
   }
   
   return true;
}

void HydraPlayer::updateMove(const Move* move)
{
   Parent::updateMove(move);

   // Convert into an extended move
   const ExtendedMove* eMove = dynamic_cast<const ExtendedMove*>(move);
   if(!eMove)
      return;

   for(U32 i=0; i<2; ++i)
   {
      mNodeDelta.posVec[i] = mWristPos[i];
      mNodeDelta.rotPrev[i] = mWristRot[i];
   }

   // Update wrist nodes with position and rotation
   for(U32 i=0; i<2; ++i)
   {
      mWristPos[i] = Point3F(eMove->posX[i], eMove->posY[i], eMove->posZ[i]);
      mWristPos[i] *= 0.001f; // The position from the move is in millimeters

      mWristRot[i] = QuatF(eMove->rotX[i], eMove->rotY[i], eMove->rotZ[i], eMove->rotW[i]);
   }

   if(isServerObject())
   {
      // As this ends up animating shape nodes, we have no sense of a transform and
      // render transform.  Therefore we treat this as the true transform and leave the
      // client shape node changes to interpolateTick() as the render transform.  Otherwise
      // on the client we'll have this node change from processTick() and then backstepping
      // and catching up to the true node change in interpolateTick(), which causes the
      // nodes to stutter.
      setWristValues(mWristPos[0], mWristRot[0], mWristPos[1], mWristRot[1]);
   }
   else
   {
      // If on the client, calc delta for backstepping
      for(U32 i=0; i<2; ++i)
      {
         mNodeDelta.pos[i] = mWristPos[i];
         mNodeDelta.posVec[i] = mNodeDelta.posVec[i] - mNodeDelta.pos[i];

         mNodeDelta.rot[i] = mWristRot[i];
      }
   }

   setMaskBits(HydraNodesUpdateMask);
}

void HydraPlayer::setWristValues(const Point3F& pos0, const QuatF& rot0, const Point3F& pos1, const QuatF& rot1)
{
   mWristPos[0] = pos0;
   mWristRot[0] = rot0;
   mWristPos[1] = pos1;
   mWristRot[1] = rot1;

   mShapeInstance->setDirty(TSShapeInstance::TransformDirty);

   mShapeInstance->animate();
}

void HydraPlayer::interpolateTick(F32 dt)
{
   Parent::interpolateTick(dt);

   // Orientation
   Point3F pos0 = mNodeDelta.pos[0] + mNodeDelta.posVec[0] * dt;
   QuatF rot0;
   rot0.interpolate(mNodeDelta.rot[0], mNodeDelta.rotPrev[0], dt);
   Point3F pos1 = mNodeDelta.pos[1] + mNodeDelta.posVec[1] * dt;
   QuatF rot1;
   rot1.interpolate(mNodeDelta.rot[1], mNodeDelta.rotPrev[1], dt);

   setWristValues(pos0, rot0, pos1, rot1);
}

void HydraPlayer::advanceTime(F32 dt)
{
   Parent::advanceTime(dt);
}

void HydraPlayer::writePacketData(GameConnection *con, BitStream *stream)
{
   // Update client regardless of status flags.
   Parent::writePacketData(con, stream);

   for(U32 i=0; i<2; ++i)
   {
      mathWrite(*stream, mWristPos[i]);
      mathWrite(*stream, mWristRot[i]);
   }
}

void HydraPlayer::readPacketData(GameConnection *con, BitStream *stream)
{
   Parent::readPacketData(con, stream);

   Point3F pos[2];
   QuatF rot[2];
   for(U32 i=0; i<2; ++i)
   {
      mathRead(*stream, &pos[i]);
      mathRead(*stream, &rot[i]);
   }

   setWristValues(pos[0], rot[0], pos[1], rot[1]);

   // Reset the client interpolation
   for(U32 i=0; i<2; ++i)
   {
      mNodeDelta.pos[i] = pos[i];
      mNodeDelta.posVec[i].zero();

      mNodeDelta.rot[i] = rot[i];
      mNodeDelta.rotPrev[i] = mNodeDelta.rot[i];
   }
}

U32 HydraPlayer::packUpdate(NetConnection *con, U32 mask, BitStream *stream)
{
   U32 retMask = Parent::packUpdate(con, mask, stream);

   // The rest of the data is part of the control object packet update.
   // If we're controlled by this client, we don't need to send it.
   // we only need to send it if this is the initial update - in that case,
   // the client won't know this is the control object yet.
   if(stream->writeFlag((NetConnection*)getControllingClient() == con && !(mask & InitialUpdateMask)))
      return(retMask);

   if(stream->writeFlag(mask & HydraNodesUpdateMask))
   {
      for(U32 i=0; i<2; ++i)
      {
         mathWrite(*stream, mWristPos[i]);
         mathWrite(*stream, mWristRot[i]);
      }
   }

   return retMask;
}

void HydraPlayer::unpackUpdate(NetConnection *con, BitStream *stream)
{
   Parent::unpackUpdate(con, stream);

   // controlled by the client?
   if(stream->readFlag())
      return;

   //HydraNodesUpdateMask
   if(stream->readFlag())
   {
      for(U32 i=0; i<2; ++i)
      {
         mathRead(*stream, &mWristPos[i]);
         mathRead(*stream, &mWristRot[i]);
      }
   }
}

void HydraPlayer::getMuzzleVector(U32 imageSlot,VectorF* vec)
{
   MatrixF mat;
   getMuzzleTransform(imageSlot,&mat);

   //GameConnection * gc = getControllingClient();
   //if (gc && !gc->isAIControlled())
   //{
   //   MountedImage& image = mMountedImageList[imageSlot];

   //   bool fp = gc->isFirstPerson();
   //   if ((fp && image.dataBlock->correctMuzzleVector) ||
   //      (!fp && image.dataBlock->correctMuzzleVectorTP))
   //   {
   //      disableHeadZCalc();
   //      if (getCorrectedAim(mat, vec))
   //      {
   //         enableHeadZCalc();
   //         return;
   //      }
   //      enableHeadZCalc();
   //   }
   //}

   mat.getColumn(1,vec);
}

//-----------------------------------------------------------------------------

HydraPlayer::TSNodeCallback::TSNodeCallback()
{
   mPlayer = NULL;
   for(U32 i=0; i<2; ++i)
   {
      mWristNodes[i] = -1;
   }
}

void HydraPlayer::TSNodeCallback::setNodeTransform(TSShapeInstance * si, S32 nodeIndex, MatrixF & localTransform)
{
   if(!mPlayer)
      return;

   for(U32 i=0; i<2; ++i)
   {
      if(nodeIndex == mWristNodes[i])
      {
         Point3F defaultPos = si->getShape()->defaultTranslations[nodeIndex];
         Quat16 defaultRot = si->getShape()->defaultRotations[nodeIndex];

         QuatF qrot(mPlayer->getWristRot(i));
         qrot *= defaultRot.getQuatF();
         qrot.setMatrix( &localTransform );      
         //localTransform.setColumn(3, defaultPos);
         //localTransform.setColumn(3, defaultPos + mPlayer->getWristPos(i));
         localTransform.setColumn(3, mPlayer->getWristPos(i));

         break;
      }
   }
}
