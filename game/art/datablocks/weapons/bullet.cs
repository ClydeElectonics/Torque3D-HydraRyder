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
// Particles
// ----------------------------------------------------------------------------
datablock ParticleData(GunFireSmoke)
{
   textureName          = "art/shapes/particles/smoke";
   dragCoefficient      = "0";
   gravityCoefficient   = "-0.1";
   windCoefficient      = 0;
   inheritedVelFactor   = "1";
   constantAcceleration = 0.0;
   lifetimeMS           = "1000";
   lifetimeVarianceMS   = 200;
   spinRandomMin = -180.0;
   spinRandomMax =  180.0;
   useInvAlpha   = true;

   colors[0]     = "0.301961 0.301961 0.301961 0.3";
   colors[1]     = "0.866142 0.866142 0.866142 0.1";
   colors[2]     = "0.897638 0.834646 0.795276 0";

   sizes[0]      = "0.1";
   sizes[1]      = "0.3";
   sizes[2]      = "1";

   times[0]      = 0.0;
   times[1]      = "0.494118";
   times[2]      = 1.0;
   animTexName = "art/shapes/particles/smoke";
};

datablock ParticleEmitterData(GunFireSmokeEmitter)
{
   ejectionPeriodMS = "4";
   periodVarianceMS = "1";
   ejectionVelocity = "3";
   velocityVariance = "0";
   thetaMin         = "0";
   thetaMax         = "5";
   lifetimeMS       = "200";
   particles = "GunFireSmoke";
   blendStyle = "NORMAL";
   softParticles = "0";
   originalName = "GunFireSmokeEmitter";
   alignParticles = "0";
   orientParticles = "0";
};

datablock ParticleData(BulletDirtDust)
{
   textureName          = "art/shapes/particles/impact";
   dragCoefficient      = "0.9";
   gravityCoefficient   = "-0.505495";
   windCoefficient      = 0;
   inheritedVelFactor   = "0";
   constantAcceleration = "-0.83";
   lifetimeMS           = "1500";
   lifetimeVarianceMS   = 300;
   spinRandomMin = -180.0;
   spinRandomMax =  180.0;
   useInvAlpha   = true;

   colors[0]     = "0.496063 0.393701 0.299213 0.67";
   colors[1]     = "0.669291 0.590551 0.511811 0.346457";
   colors[2]     = "0.897638 0.84252 0.795276 0";

   sizes[0]      = "0.5";
   sizes[1]      = "1";
   sizes[2]      = "1.5";

   times[0]      = 0.0;
   times[1]      = "0.494118";
   times[2]      = 1.0;
   animTexName = "art/shapes/particles/impact";
};

datablock ParticleEmitterData(BulletDirtDustEmitter)
{
   ejectionPeriodMS = "2";
   periodVarianceMS = "1";
   ejectionVelocity = "4";
   velocityVariance = 1.0;
   thetaMin         = 0.0;
   thetaMax         = "30";
   lifetimeMS       = "5";
   particles = "BulletDirtDust";
   blendStyle = "NORMAL";
   lifetimeVarianceMS = "4";
   phiVariance = "180";
};

//-----------------------------------------------------------------------------
// Explosion
//-----------------------------------------------------------------------------
datablock ExplosionData(BulletDirtExplosion)
{
   soundProfile = BulletImpactSound;
   lifeTimeMS = 65;

   // Point emission
   emitter[0] = BulletDirtDustEmitter;
   emitter[1] = BulletDirtSprayEmitter;
   emitter[2] = BulletDirtRocksEmitter;
};

//--------------------------------------------------------------------------
// Shell ejected during reload.
//-----------------------------------------------------------------------------
datablock DebrisData(BulletShell)
{
   shapeFile = "art/shapes/weapons/RifleShell/RifleShell.DAE";
   lifetime = 6.0;
   minSpinSpeed = 300.0;
   maxSpinSpeed = 400.0;
   elasticity = 0.65;
   friction = 0.05;
   numBounces = 5;
   staticOnMaxBounce = true;
   snapOnMaxBounce = false;
   ignoreWater = true;
   fade = true;
};

//-----------------------------------------------------------------------------
// Projectile Object
//-----------------------------------------------------------------------------
datablock LightDescription( BulletProjectileLightDesc )
{
   color  = "0.0 0.5 0.7";
   range = 3.0;
};

datablock ProjectileData( BulletProjectile )
{
   projectileShapeName = "";

   directDamage        = 5;
   radiusDamage        = 0;
   damageRadius        = 0.5;
   areaImpulse         = 0.5;
   
   // A force multiplier when using a physics plug-in
   impactForce         = 0.1; //1;

   explosion           = BulletDirtExplosion;
   decal               = BulletHoleDecal;

   muzzleVelocity      = 120;
   velInheritFactor    = 1;

   armingDelay         = 0;
   lifetime            = 992;
   fadeDelay           = 1472;
   bounceElasticity    = 0;
   bounceFriction      = 0;
   isBallistic         = false;
   gravityMod          = 1;
};

function BulletProjectile::onCollision(%this,%obj,%col,%fade,%pos,%normal)
{
   // Apply impact force from the projectile.
   
   // Make those cans fly!
   if(%col.isInNamespaceHierarchy("TinCanClass"))
   {
      // While the physics simulation handles the bullet hit just fine, it is
      // fun to give the cans some upwards motion too.  This makes it
      // more gamey.
      %impulse = "0 0 2";
      %col.applyImpulse(%col.getPosition(), %impulse);
   }
      
   // If there is a sound associated with the hit object, then play it
   if(%col.hitSound $= "metal")
   {
      // Make sure we don't play the same sound in a row
      %snd = getRandom(0, 4);
      while(%snd == $LastMetalPlateSoundPlayed)
      {
         %snd = getRandom(0, 4);
      }
      $LastMetalPlateSoundPlayed = %snd;
      
      switch(%snd)
      {
         case 0:
            ServerPlay3D(MetalPlateHit01Snd, %pos SPC "1 0 0 0");
            
         case 1:
            ServerPlay3D(MetalPlateHit02Snd, %pos SPC "1 0 0 0");
            
         case 2:
            ServerPlay3D(MetalPlateHit03Snd, %pos SPC "1 0 0 0");
            
         case 3:
            ServerPlay3D(MetalPlateHit04Snd, %pos SPC "1 0 0 0");
            
         case 4:
            ServerPlay3D(MetalPlateHit05Snd, %pos SPC "1 0 0 0");
      }
   }
}
