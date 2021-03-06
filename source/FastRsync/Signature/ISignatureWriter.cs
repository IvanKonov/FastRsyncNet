using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FastRsync.Core;
using FastRsync.Hash;
using Newtonsoft.Json;

namespace FastRsync.Signature
{
    public interface ISignatureWriter
    {
        void WriteMetadata(SignatureMetadata metadata);
        Task WriteMetadataAsync(SignatureMetadata metadata);
        void WriteChunk(ChunkSignature signature);
        Task WriteChunkAsync(ChunkSignature signature);
    }

    public class SignatureWriter : ISignatureWriter
    {
        private readonly BinaryWriter signaturebw;

        public SignatureWriter(Stream signatureStream)
        {
            this.signaturebw = new BinaryWriter(signatureStream);
        }

        private static void WriteMetadataInternal(BinaryWriter bw, SignatureMetadata metadata)
        {
            bw.Write(FastRsyncBinaryFormat.SignatureHeader);
            bw.Write(FastRsyncBinaryFormat.Version);
            var metadataStr = JsonConvert.SerializeObject(metadata, JsonSerializationSettings.JsonSettings);
            bw.Write(metadataStr);
        }

        public void WriteMetadata(SignatureMetadata metadata)
        {
            WriteMetadataInternal(signaturebw, metadata);
        }

        public async Task WriteMetadataAsync(SignatureMetadata metadata)
        {
            var ms = new MemoryStream(256);
            var msbw = new BinaryWriter(ms);
            WriteMetadataInternal(msbw, metadata);
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(signaturebw.BaseStream).ConfigureAwait(false);
        }

        public void WriteChunk(ChunkSignature signature)
        {
            signaturebw.Write(signature.Length);
            signaturebw.Write(signature.RollingChecksum);
            signaturebw.Write(signature.Hash);
        }

        public async Task WriteChunkAsync(ChunkSignature signature)
        {
            signaturebw.Write(signature.Length);
            signaturebw.Write(signature.RollingChecksum);
            await signaturebw.BaseStream.WriteAsync(signature.Hash, 0, signature.Hash.Length).ConfigureAwait(false);
        }
    }
}