using ArchivistClient;

namespace GatewayClient
{
    // This one's a little weird:
    // Map the gatewayAPI types to the archivistAPI types.
    // They should match 100% because the gateway simply forwards.
    // After that, we use the ArchivistClient mapper to map to non-volatile types.
    public class Mapper
    {
        private readonly ArchivistClient.Mapper submapper = new ArchivistClient.Mapper();

        public LocalDataset Map(GatewayApi.DataItem dataItem)
        {
            return submapper.Map(new ArchivistOpenApi.DataItem
            {
                Cid = dataItem.Cid,
                Manifest = Map(dataItem.Manifest),
                AdditionalProperties = dataItem.AdditionalProperties
            });
        }

        private ArchivistOpenApi.ManifestItem Map(GatewayApi.ManifestItem manifest)
        {
            return new ArchivistOpenApi.ManifestItem
            {
                BlockSize = manifest.BlockSize,
                DatasetSize = manifest.DatasetSize,
                Filename = manifest.Filename,
                Mimetype = manifest.Mimetype,
                Protected = manifest.Protected,
                TreeCid = manifest.TreeCid,
                AdditionalProperties = manifest.AdditionalProperties
            };
        }
    }
}
