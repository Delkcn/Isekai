using System;
using System.Drawing;
namespace JouchoPet {
 enum CharacterForm { AnemoneI, AnemoneII }
 // Appearance selects textures only. Bone registrations and action timelines are shared.
 static class CharacterForms {
  internal static void Validate(CharacterForm form) {
   if(form!=CharacterForm.AnemoneI && form!=CharacterForm.AnemoneII)throw new ArgumentOutOfRangeException("form");
  }
  internal static string Name(CharacterForm form){Validate(form);return form==CharacterForm.AnemoneI?"Anemone I":"Anemone II";}
  internal static string Resource(string original,CharacterForm form){
   Validate(form);if(form==CharacterForm.AnemoneI)return original;
   switch(original.Replace('\\','/')){
    case "v0610/anemone-i-unified-v10-eyes.png":return "v0800-a2/anemone-ii-atlas.png";
    case "v064/walk-legs.png":return "v0800-a2/walk-legs.png";
    case "v067/sleep-keys-eye.png":return "v0800-a2/sleep-keys.png";
    case "v0616/greeting-underlay.png":return "v0800-a2/greeting-underlay.png";
    case "v0611/underarm-backplate.png":return "v0800-a2/underarm-backplate-hires.png";
    default:return original; // Skin-only hands and weapons are intentionally shared.
   }
  }
  internal static void CheckResources(CharacterForm form){
   Validate(form);if(form==CharacterForm.AnemoneI)return;
   string[] names={CharacterArt.Resource,"v064/walk-legs.png","v067/sleep-keys-eye.png","v0616/greeting-underlay.png","v0611/underarm-backplate.png"};
   Size[] sizes={new Size(1774,887),new Size(1199,1312),new Size(1774,887),new Size(1134,1387),new Size(1402,1122)};
   for(int i=0;i<names.Length;i++)using(var image=AssetStore.Image(names[i],form))
    if(image.Size!=sizes[i])throw new InvalidOperationException("Unexpected A2 resource dimensions: "+Resource(names[i],form));
   using(var front=AssetStore.Image(A2FrontArt.Resource))if(front.Width<380||front.Height<465)throw new InvalidOperationException("A2 front resource is too small.");
   using(var head=AssetStore.Image(A2FrontArt.HeadResource))if(head.Size!=new Size(1355,1161))throw new InvalidOperationException("Unexpected A2 approved head dimensions.");
  }
 }
}
