using System;
using Microsoft.Xna.Framework.Content.Pipeline;
using NUnit.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
#if DIRECTX
using System.Collections.Generic;
using TwoMGFX;
#endif

namespace MonoGame.Tests.ContentPipeline
{
    [Category("Effects")]
    class EffectProcessorTests
    {
        class ImporterContext : ContentImporterContext
        {
            public override string IntermediateDirectory
            {
                get { throw new NotImplementedException(); }
            }

            public override ContentBuildLogger Logger
            {
                get { throw new NotImplementedException(); }
            }

            public override string OutputDirectory
            {
                get { throw new NotImplementedException(); }
            }

            public override void AddDependency(string filename)
            {
                throw new NotImplementedException();
            }
        }

#if DIRECTX
        [Test]
        public void TestPreprocessor()
        {
            var effectFile = "Assets/Effects/PreprocessorTest.fx";
            var effectCode = File.ReadAllText(effectFile);
            var fullPath = Path.GetFullPath(effectFile);

            // Preprocess.
            var mgDependencies = new List<string>();
            var mgPreprocessed = Preprocessor.Preprocess(effectCode, fullPath, new Dictionary<string, string>
            {
                { "TEST2", "1" }
            }, mgDependencies, new TestEffectCompilerOutput());

            Assert.That(mgDependencies, Has.Count.EqualTo(1));
            Assert.That(Path.GetFileName(mgDependencies[0]), Is.EqualTo("PreprocessorInclude.fxh"));

            Assert.That(mgPreprocessed, Does.Not.Contain("Foo"));
            Assert.That(mgPreprocessed, Does.Contain("Bar"));
            Assert.That(mgPreprocessed, Does.Not.Contain("Baz"));

            Assert.That(mgPreprocessed, Does.Contain("FOO"));
            Assert.That(mgPreprocessed, Does.Not.Contain("BAR"));

            // Check that we can actually compile this file.
            BuildEffect(effectFile, TargetPlatform.Windows);
        }

        private class TestEffectCompilerOutput : IEffectCompilerOutput
        {
            public void WriteWarning(string file, int line, int column, string message)
            {
                Console.WriteLine("Warning: {0}({1},{2}): {3}", file, line, column, message);
            }

            public void WriteError(string file, int line, int column, string message)
            {
                Console.WriteLine("Error: {0}({1},{2}): {3}", file, line, column, message);
            }
        }
#endif

        [Test]
        [TestCase("Assets/Effects/ParserTest.fx")]
        public void TestParser(string effectFile)
        {
            BuildEffect(effectFile, TargetPlatform.Windows);
        }

        [Test]
        public void BuildTessellationEffect()
        {
            var output = BuildEffect("Assets/Effects/Tessellation.fx", TargetPlatform.Windows);
            var stages = ReadShaderStages(output.GetEffectCode());

            Assert.That(stages, Is.EqualTo(new[]
            {
                ShaderStage.Pixel,
                ShaderStage.Vertex,
                ShaderStage.Hull,
                ShaderStage.Domain,
            }));
        }

        [Test]
        public void TestDefines()
        {
            Assert.DoesNotThrow(() => BuildEffect("Assets/Effects/DefinesTest.fx", TargetPlatform.Windows, "MACRO_DEFINE_TEST=3"));
            Assert.Throws<InvalidContentException>(() =>
                BuildEffect("Assets/Effects/DefinesTest.fx", TargetPlatform.Windows, "MACRO_DEFINE_TEST=4"));
            Assert.Throws<InvalidContentException>(() =>
                BuildEffect("Assets/Effects/DefinesTest.fx", TargetPlatform.Windows));
            Assert.Throws<InvalidContentException>(() =>
                BuildEffect("Assets/Effects/DefinesTest.fx", TargetPlatform.Windows, "INVALID_SYNTAX;ANOTHER_MACRO;MACRO_DEFINE_TEST=3"));
        }

        [Test]
        [TestCase("Assets/Effects/Stock/AlphaTestEffect.fx")]
        [TestCase("Assets/Effects/Stock/BasicEffect.fx")]
        [TestCase("Assets/Effects/Stock/DualTextureEffect.fx")]
        [TestCase("Assets/Effects/Stock/EnvironmentMapEffect.fx")]
        [TestCase("Assets/Effects/Stock/SkinnedEffect.fx")]
        [TestCase("Assets/Effects/Stock/SpriteEffect.fx")]
        public void BuildStockEffect(string effectFile)
        {
            BuildEffect(effectFile, TargetPlatform.Windows);
        }

        private CompiledEffectContent BuildEffect(string effectFile, TargetPlatform targetPlatform, string defines = null)
        {
            var importerContext = new ImporterContext();
            var importer = new EffectImporter();
            var input = importer.Import(effectFile, importerContext);

            Assert.NotNull(input);

            var processorContext = new TestProcessorContext(targetPlatform, Path.ChangeExtension(effectFile, ".xnb"));
            var processor = new EffectProcessor { Defines = defines };
            var output = processor.Process(input, processorContext);

            Assert.NotNull(output);

            // TODO: Should we test the writer?
            return output;
        }

        private static ShaderStage[] ReadShaderStages(byte[] effectCode)
        {
            using (var reader = new BinaryReader(new MemoryStream(effectCode)))
            {
                Assert.That(new string(reader.ReadChars(4)), Is.EqualTo("MGFX"));
                Assert.That(reader.ReadByte(), Is.EqualTo(12));
                reader.ReadByte();
                reader.ReadInt32();

                var constantBufferCount = reader.ReadInt32();
                for (var i = 0; i < constantBufferCount; i++)
                {
                    reader.ReadString();
                    reader.ReadUInt16();
                    var parameterCount = reader.ReadInt32();
                    for (var p = 0; p < parameterCount; p++)
                    {
                        reader.ReadInt32();
                        reader.ReadUInt16();
                    }
                }

                var shaderCount = reader.ReadInt32();
                var stages = new ShaderStage[shaderCount];
                for (var i = 0; i < shaderCount; i++)
                {
                    stages[i] = (ShaderStage)reader.ReadByte();
                    reader.ReadString();
                    reader.ReadString();
                    reader.ReadBytes(reader.ReadInt32());

                    var samplerCount = reader.ReadByte();
                    Assert.That(samplerCount, Is.Zero, "The probe effect should not contain samplers.");

                    reader.ReadBytes(reader.ReadByte());

                    var attributeCount = reader.ReadByte();
                    for (var a = 0; a < attributeCount; a++)
                    {
                        reader.ReadString();
                        reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadInt16();
                    }
                }

                return stages;
            }
        }
    }
}
