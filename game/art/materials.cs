
singleton Material(newMaterial)
{
   mapTo = "unmapped_mat";
   diffuseMap[0] = "art/environment/grass1.png";
};

singleton Material(GrassSprigs1)
{
   mapTo = "unmapped_mat";
   diffuseMap[0] = "art/environment/sprigs.png";
   materialTag0 = "RoadAndPath";
   translucent = "1";
   translucentZWrite = "1";
   alphaTest = "0";
   alphaRef = "255";
};
