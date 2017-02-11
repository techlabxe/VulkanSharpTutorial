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
    }
}
