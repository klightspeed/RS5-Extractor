using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace RS5_Extractor
{
    class Program
    {
        private static RS5Directory ProcessRS5File(Stream filestrm)
        {
            filestrm.Seek(0, SeekOrigin.Begin);
            byte[] fileheader = new byte[24];
            filestrm.Read(fileheader, 0, 24);
            string magic = Encoding.ASCII.GetString(fileheader, 0, 8);
            if (magic == "CFILEHDR")
            {
                long directory_offset = BitConverter.ToInt64(fileheader, 8);
                int dirent_length = BitConverter.ToInt32(fileheader, 16);
                return new RS5Directory(filestrm, directory_offset, dirent_length);
            }
            else
            {
                throw new InvalidDataException("File is not an RS5 file");
            }
        }

        private static Dictionary<string, List<AnimationClip>> ProcessEnvironmentAnimations(RS5Environment environ)
        {
            Dictionary<string, List<AnimationClip>> animclips = new Dictionary<string, List<AnimationClip>>(StringComparer.InvariantCultureIgnoreCase);
            
            foreach (KeyValuePair<string, dynamic> animal_kvp in environ.Data["animals"])
            {
                string key = animal_kvp.Key;
                dynamic animal = animal_kvp.Value;
                string modelset = "animals\\" + key;
                string modelname = animal["model"];
                if (modelset.ToLower().StartsWith(modelname.ToLower()))
                {
                    modelset = modelset.Substring(modelname.Length);
                    if (modelset.StartsWith("_"))
                    {
                        modelset = modelset.Substring(1);
                    }
                }
                modelset = Path.GetFileName(modelset);
                Dictionary<string, dynamic> animations = animal["animations"];
                foreach (KeyValuePair<string, dynamic> anim_kvp in animations)
                {
                    string animname = (modelset == "" ? "" : (modelset + ".")) + anim_kvp.Key;
                    string animparamstring = anim_kvp.Value;
                    Dictionary<string, string> animparams = animparamstring.Split(',').Select(v => v.Split(new char[] { '=' }, 2)).Where(v => v.Length == 2).ToDictionary(v => v[0], v => v[1]);
                    int startframe = Int32.Parse(animparams["F1"]);
                    int endframe = Int32.Parse(animparams["F2"]);
                    float framerate = animparams.ContainsKey("rate") ? Single.Parse(animparams["rate"]) : 60.0F;
                    if (!animclips.ContainsKey(modelname))
                    {
                        animclips[modelname] = new List<AnimationClip>();
                    }
                    animclips[modelname].Add(new AnimationClip { ModelName = modelname, Name = animname, StartFrame = startframe, NumFrames = endframe - startframe, FrameRate = framerate });
                }
            }

            foreach (KeyValuePair<string, dynamic> movement_kvp in environ.Data["creature"]["navigation"]["MOVEMENTS"])
            {
                string animname = movement_kvp.Key;
                string animparamstring = movement_kvp.Value;
                Dictionary<string, string> animparams = animparamstring.Split(',').Select(v => v.Split(new char[] { '=' }, 2)).Where(v => v.Length == 2).ToDictionary(v => v[0], v => v[1], StringComparer.InvariantCultureIgnoreCase);
                if (animparams.ContainsKey("F1") && animparams.ContainsKey("F2"))
                {
                    int startframe = Int32.Parse(animparams["F1"]);
                    int endframe = Int32.Parse(animparams["F2"]);
                    float framerate = animparams.ContainsKey("rate") ? Single.Parse(animparams["rate"]) : 60.0F;
                    foreach (string modelname in new string[]{ "creature", "creature_ik" })
                    {
                        if (!animclips.ContainsKey(modelname))
                        {
                            animclips[modelname] = new List<AnimationClip>();
                        }
                        animclips[modelname].Add(new AnimationClip { ModelName = modelname, Name = animname, StartFrame = startframe, NumFrames = endframe - startframe, FrameRate = framerate });
                    }
                }
            }

            foreach (KeyValuePair<string, dynamic> handanim_kvp in environ.Data["player"]["hand_animations"])
            {
                string animname = handanim_kvp.Key;
                Dictionary<string, dynamic> animparams = handanim_kvp.Value;
                if (animparams.ContainsKey("frames"))
                {
                    int startframe = animparams["frames"][0];
                    int endframe = animparams["frames"][1];
                    float framerate = 24.0F;
                    foreach (string modelname in new string[] { "hands", "hands_synth", "hands_ending" })
                    {
                        if (!animclips.ContainsKey(modelname))
                        {
                            animclips[modelname] = new List<AnimationClip>();
                        }
                        animclips[modelname].Add(new AnimationClip { ModelName = modelname, Name = animname, StartFrame = startframe, NumFrames = endframe - startframe, FrameRate = framerate });
                    }
                }
            }

            foreach (KeyValuePair<string, dynamic> obj_kvp in environ.Data["game_objects"])
            {
                string animname = obj_kvp.Key;
                Dictionary<string, dynamic> obj = obj_kvp.Value;
                if (obj.ContainsKey("SCRIPT"))
                {
                    Dictionary<string, dynamic> script = obj["SCRIPT"];
                    if (script.ContainsKey("frames"))
                    {
                        int startframe = script["frames"][0];
                        int endframe = script["frames"][1];

                        foreach (string modelname in new string[] { "hands", "hands_synth", "hands_ending" })
                        {

                            float framerate = 24.0F;
                            if (!animclips.ContainsKey(modelname))
                            {
                                animclips[modelname] = new List<AnimationClip>();
                            }
                            animclips[modelname].Add(new AnimationClip { ModelName = modelname, Name = animname, StartFrame = startframe, NumFrames = endframe - startframe, FrameRate = framerate });
                        }
                    }
                }
            }

            return animclips;
        }

        private static void Main(string[] args)
        {
            Stream main_filestrm = File.Open("main.rs5", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Console.Write("Processing main.rs5 central directory ... ");
            RS5Directory main_rs5 = ProcessRS5File(main_filestrm);
            Console.WriteLine("Done");
            Console.Write("Processing environment.rs5 ... ");
            Stream environ_filestrm = File.Open("environment.rs5", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            RS5Directory environ_rs5 = ProcessRS5File(environ_filestrm);
            RS5Chunk environ_chunk = environ_rs5["environment"].Data;
            RS5Environment environ = new RS5Environment(environ_chunk.Chunks["DATA"].Data);
            Dictionary<string, List<AnimationClip>> animclips = ProcessEnvironmentAnimations(environ);
            Console.WriteLine("Done");

            Console.WriteLine("Processing Textures ... ");
            foreach (KeyValuePair<string, RS5DirectoryEntry> dirent in main_rs5.Where(d => d.Value.Type == "IMAG"))
            {
                Texture.AddTexture(dirent.Value);
                Texture texture = Texture.GetTexture(dirent.Key);
                if (!File.Exists(texture.PNGFilename) && !File.Exists(texture.DDSFilename))
                {
                    Console.WriteLine("Saving texture {0}", dirent.Key);
                    texture.Save();
                }
            }

            Console.WriteLine("Processing Immobile Models ... ");
            foreach (KeyValuePair<string, RS5DirectoryEntry> dirent in main_rs5.Where(d => d.Value.Type == "IMDL"))
            {
                ImmobileModel model = new ImmobileModel(dirent.Value);
                if (!File.Exists(model.ColladaMultimeshFilename))
                {
                    Console.WriteLine("Saving immobile model {0}", dirent.Key);
                    model.SaveUnanimated();
                    model.SaveMultimesh();
                }
            }

            Console.WriteLine("Processing Animated Models ... ");
            foreach (KeyValuePair<string, RS5DirectoryEntry> dirent in main_rs5.Where(d => d.Value.Type == "AMDL"))
            {
                AnimatedModel model = new AnimatedModel(dirent.Value);
                if (!File.Exists(model.ColladaMultimeshFilename))
                {
                    Console.WriteLine("Saving animated model {0}", dirent.Key);
                    if (!model.HasGeometry)
                    {
                        Console.WriteLine("Model {0} has no geometry");
                    }
                    else
                    {
                        model.SaveUnanimated();

                        if (model.HasSkeleton && model.IsAnimated)
                        {
                            Console.WriteLine("  Saving all animations ({0} frames @ 24 fps)", model.NumAnimationFrames);
                            model.SaveAnimation("ALL", 0, model.NumAnimationFrames, 24.0F);

                            if (animclips.ContainsKey(model.Name))
                            {
                                foreach (AnimationClip clip in animclips[model.Name])
                                {
                                    Console.WriteLine("  Saving animation {0} ({1} frames @ {2} fps)", clip.Name, clip.NumFrames, clip.FrameRate);
                                    model.SaveAnimation(clip.Name, clip.StartFrame, clip.NumFrames, clip.FrameRate);
                                }
                            }
                            else
                            {
                                Console.WriteLine("  model clip start and end frames not in environment");
                            }
                        }

                        model.SaveMultimesh();
                    }
                }
            }
        }
    }
}
