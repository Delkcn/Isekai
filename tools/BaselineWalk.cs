using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using JouchoPet;
class BaselineWalk {
 static void Main() {
  foreach(int size in new[]{220,300,400}) {
   Size c=PetRenderer.CanvasSize(size);
   using(var side=new SideWalk(c.Width,c.Height,size))using(var sha=SHA256.Create()) {
    foreach(int facing in new[]{-1,1})foreach(double phase in new[]{0.0,.12,.35,.65,.7,.85,.999})foreach(double settle in new[]{0.0,.5,1.0})foreach(double carry in new[]{0.0,.65}) {
     int[] pixels=PoseMesh.Read(side.Draw(.4,phase,.8,facing,settle,carry));var data=new byte[pixels.Length*4];Buffer.BlockCopy(pixels,0,data,0,data.Length);sha.TransformBlock(data,0,data.Length,null,0);
    }
    sha.TransformFinalBlock(new byte[0],0,0);string hash=BitConverter.ToString(sha.Hash).Replace("-","");File.WriteAllText("walk-"+size+".sha256",hash);Console.WriteLine(size+" "+hash);
   }
  }
 }
}
