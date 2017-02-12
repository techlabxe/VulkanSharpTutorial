using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpVulkan;
using System.IO;

namespace VulkanSharpTutorial.Common
{
    static class SampleHelpers
    {
        /// <summary>
        /// イメージレイアウトの変更用の関数
        /// </summary>
        /// <param name="command"></param>
        /// <param name="srcAccessMask"></param>
        /// <param name="dstAccessMask"></param>
        /// <param name="oldLayout"></param>
        /// <param name="newLayout"></param>
        /// <param name="image"></param>
        /// <param name="aspectFlags"></param>

        public static void setImageMemoryBarrier(
            VkCommandBuffer command,
            VkAccessFlags srcAccessMask, VkAccessFlags dstAccessMask,
            VkImageLayout oldLayout, VkImageLayout newLayout,
            VkImage image, VkImageAspectFlags aspectFlags)
        {
            var imageMemoryBarrier = new VkImageMemoryBarrier()
            {
                srcAccessMask = srcAccessMask,
                dstAccessMask = dstAccessMask,
                oldLayout = oldLayout,
                newLayout = newLayout,
                srcQueueFamilyIndex = ~0u,
                dstQueueFamilyIndex = ~0u,
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = aspectFlags,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1,
                },
                image = image
            };
            VulkanAPI.vkCmdPipelineBarrier(
                command,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
                VkPipelineStageFlags.VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,
                0,
                null, // MemoryBarriers
                null, // BufferMemoryBarriers
                new VkImageMemoryBarrier[] { imageMemoryBarrier }
                );
        }

        /// <summary>
        /// バッファの作成
        /// </summary>
        /// <param name="device"></param>
        /// <param name="physicalDevice"></param>
        /// <param name="bufferSize"></param>
        /// <param name="usageFlags"></param>
        /// <param name="memoryFlags"></param>
        /// <param name="buffer"></param>
        /// <param name="memory"></param>
        public static void CreateBuffer(VkDevice device, VkPhysicalDevice physicalDevice, int bufferSize, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags memoryFlags, out VkBuffer buffer, out VkDeviceMemory memory)
        {
            buffer = null;
            memory = null;
            var bufferCreateInfo = new VkBufferCreateInfo(usageFlags, bufferSize);
            if (VkResult.VK_SUCCESS != VulkanAPI.vkCreateBuffer(device, ref bufferCreateInfo, out buffer))
            {
                return;
            }
            VkMemoryRequirements requirements;
            VulkanAPI.vkGetBufferMemoryRequirements(device, buffer, out requirements);
            if (VkResult.VK_SUCCESS != VulkanAPI.vkAllocateMemory(device, physicalDevice, ref requirements, memoryFlags, out memory))
            {
                return;
            }
            VulkanAPI.vkBindBufferMemory(device, buffer, memory, 0);
        }


        /// <summary>
        /// SPVシェーダーファイルを読み込んで VkPipelineShaderStageCreateInfo を作成します.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="shaderFile"></param>
        /// <param name="stageFlag"></param>
        /// <returns></returns>
        public static VkPipelineShaderStageCreateInfo CreateShader(VkDevice device, string shaderFile, VkShaderStageFlagBits stageFlag)
        {
            var stageCreateInfo = new VkPipelineShaderStageCreateInfo();
            using (var fs = new FileStream(shaderFile, FileMode.Open, FileAccess.Read))
            {
                var code = new byte[fs.Length];
                fs.Read(code, 0, (int)fs.Length);

                var shaderModuleCreateInfo = new VkShaderModuleCreateInfo()
                {
                    shaderCodeBinary = code,
                };

                VkShaderModule shaderModule;
                VulkanAPI.vkCreateShaderModule(device, ref shaderModuleCreateInfo, out shaderModule);

                stageCreateInfo.flags = 0;
                stageCreateInfo.stage = stageFlag;
                stageCreateInfo.pName = "main";
                stageCreateInfo.module = shaderModule;
            }
            return stageCreateInfo;
        }

        /// <summary>
        /// ユニフォームバッファのディスクリプタの生成.
        /// </summary>
        /// <param name="destBinding"></param>
        /// <param name="uniformBuffer"></param>
        /// <param name="size"></param>
        /// <param name="descriptorSet"></param>
        /// <returns></returns>
        public static VkWriteDescriptorSet CreateDescriptorFromUniformBuffer( uint destBinding, VkDescriptorBufferInfo descUniformBufferInfo, VkDescriptorSet descriptorSet )
        {
            var descriptorUniform = new VkWriteDescriptorSet()
            {
                descriptorCount=1,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
                pBufferInfo = new[] { descUniformBufferInfo, },
                dstBinding = destBinding,
                dstSet = descriptorSet
            };
            return descriptorUniform;
        }

        /// <summary>
        /// テクスチャのためのディスクリプタの生成.
        /// </summary>
        /// <param name="destBinding"></param>
        /// <param name="imageView"></param>
        /// <param name="sampler"></param>
        /// <param name="descriptorSet"></param>
        /// <returns></returns>
        public static VkWriteDescriptorSet CreateDescriptorFromImageSampler( uint destBinding, VkDescriptorImageInfo descImageSampler, VkDescriptorSet descriptorSet )
        {
            var descriptorImageSampler = new VkWriteDescriptorSet()
            {
                descriptorCount = 1,
                descriptorType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                pImageInfo = new[] { descImageSampler },
                dstBinding = destBinding,
                dstSet = descriptorSet,
            };
            return descriptorImageSampler;
        }


        public static void CreateTexture(VkDevice device, VkPhysicalDevice physicalDevice, int width, int height, byte[] imageSource, out VkImage image, out VkDeviceMemory memory, VkQueue queue, VkCommandBuffer workCommandBuffer )
        {
            var imageCreateInfo = new VkImageCreateInfo()
            {
                imageType = VkImageType.VK_IMAGE_TYPE_2D,
                format = VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
                extent = new VkExtent3D(width, height),
                mipLevels = 1,
                usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            };
            VulkanAPI.vkCreateImage(device, ref imageCreateInfo, out image);
            VkMemoryRequirements requirements;
            VulkanAPI.vkGetImageMemoryRequirements(device, image, out requirements);
            VkMemoryPropertyFlags memoryFlags = VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT;
            VulkanAPI.vkAllocateMemory(device, physicalDevice, ref requirements, memoryFlags, out memory);
            VulkanAPI.vkBindImageMemory(device, image, memory, 0);

            // ステージングバッファ経由で転送.
            VkBuffer staging;
            VkDeviceMemory stagingMemory;
            var stagingFlags = VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT;
            CreateBuffer(device, physicalDevice, imageSource.Length, VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT, stagingFlags, out staging, out stagingMemory);
            MappedMemoryStream mappedStream;
            VulkanAPI.vkMapMemory(device, stagingMemory, 0, VkDeviceSize.VK_WHOLE_SIZE, 0, out mappedStream);
            mappedStream.Write(imageSource);
            VulkanAPI.vkUnmapMemory(device, stagingMemory);

            var copyRegion = new VkBufferImageCopy()
            {
                imageExtent = new VkExtent3D(width, height),
                bufferImageHeight = (uint)height,
                imageSubresource = new VkImageSubresourceLayers()
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1,
                }
            };

            // 一時的なコマンドバッファで転送処理.
            var command = workCommandBuffer;
            VulkanAPI.vkBeginCommandBuffer(command);
            setImageMemoryBarrier(command,
                    0, VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
                    VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                    image, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
            VulkanAPI.vkCmdCopyBufferToImage(command, staging, image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, new[] { copyRegion });
            setImageMemoryBarrier(command,
                    VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT, VkAccessFlags.VK_ACCESS_SHADER_READ_BIT,
                    VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                    image, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
            VulkanAPI.vkEndCommandBuffer(command);

            var submitInfo = new VkSubmitInfo()
            {
                commandBuffers = new[] { command },
            };
            var fenceCreateInfo = new VkFenceCreateInfo() { };
            VkFence fence;
            VulkanAPI.vkCreateFence(device, fenceCreateInfo, out fence);
            VulkanAPI.vkQueueSubmit(queue, new[] { submitInfo }, fence);

            VulkanAPI.vkWaitForFences(device, new[] { fence }, true, ulong.MaxValue);
            VulkanAPI.vkDestroyFence(device, fence);
            VulkanAPI.vkDestroyBuffer(device, staging);
            VulkanAPI.vkFreeMemory(device, stagingMemory);
        }
    }

    /// <summary>
    /// 32bit カラー TGA ファイルを読み取るだけの簡単クラス.
    /// </summary>
    public class SimpleTgaReader
    {
        public SimpleTgaReader(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                var data = new byte[fs.Length];
                fs.Read(data, 0, (int)fs.Length);

                var reader = new BinaryReader(new MemoryStream(data));

                reader.BaseStream.Seek(12, SeekOrigin.Current);

                Width = reader.ReadInt16();
                Height = reader.ReadInt16();

                reader.BaseStream.Seek(2, SeekOrigin.Current);

                var imageBytes = Width * Height * 4;
                ImageData = reader.ReadBytes(imageBytes);
            }
        }

        public byte[] ImageData { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
    }
}
