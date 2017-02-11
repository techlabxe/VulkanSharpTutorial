using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpVulkan;

namespace SimpleClear
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 各イベントを繋ぐ.
            vkctrl.VulkanInitialized += Vkctrl_VulkanInitialized;
            vkctrl.VulkanRendering += Vkctrl_VulkanRendering;
            vkctrl.VulkanClosing += Vkctrl_VulkanClosing;
            Closing += MainWindow_Closing;
        }
        private void Vkctrl_VulkanInitialized(object sender, SharpVulkanWpf.VulkanEventArgs args)
        {
            var device = args.Device;
            var commandPoolCreateInfo = new VkCommandPoolCreateInfo()
            {
                queueFamilyIndex = args.GraphicsQueueIndex,
                flags = VkCommandPoolCreateFlags.VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT,
            };
            VulkanAPI.vkCreateCommandPool(device, ref commandPoolCreateInfo, out m_commandPool);

            var allocateInfo = new VkCommandBufferAllocateInfo()
            {
                commandBufferCount = 1,
                commandPool = m_commandPool,
            };
            VulkanAPI.vkAllocateCommandBuffers(device, ref allocateInfo, out m_commandBuffers);
        }

        private void Vkctrl_VulkanRendering(object sender, SharpVulkanWpf.VulkanEventArgs args)
        {
            var device = args.Device;
            var image = vkctrl.AcquireNextImage();

            var command = m_commandBuffers[0];
            VulkanAPI.vkBeginCommandBuffer(command);

            VkClearColorValue clearColor = new VkClearColorValue();
            clearColor.valF32.R = 0.125f;
            clearColor.valF32.G = 0.25f;
            clearColor.valF32.B = (float)(0.5f * Math.Sin(m_frameCount * 0.1) + 0.5);
            clearColor.valF32.A = 1.0f;

            VkImageSubresourceRange range = new VkImageSubresourceRange()
            {
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1,
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT
            };

            setImageMemoryBarrier(command,
                VkAccessFlags.VK_ACCESS_MEMORY_READ_BIT, VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                image, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);

            VulkanAPI.vkCmdClearColorImage(command,
                image,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                ref clearColor,
                new VkImageSubresourceRange[] { range });


            // Present のためにレイアウト変更
            setImageMemoryBarrier(command,
                VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT, VkAccessFlags.VK_ACCESS_MEMORY_READ_BIT,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,
                image, VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
            VulkanAPI.vkEndCommandBuffer(command);

            var submitInfo = new VkSubmitInfo()
            {
                commandBuffers = new[] { command }
            };
            VulkanAPI.vkQueueSubmit(args.GraphicsQueue, new VkSubmitInfo[] { submitInfo }, null);

            vkctrl.SwapBuffers();
            m_frameCount++;
        }
        private void Vkctrl_VulkanClosing(object sender, SharpVulkanWpf.VulkanEventArgs args)
        {
            // 本クラスで作ったVulkanの各リソースを破棄する.
            var dev = args.Device;
            VulkanAPI.vkFreeCommandBuffers(dev, m_commandPool, m_commandBuffers);
            VulkanAPI.vkDestroyCommandPool(dev, m_commandPool);
            m_commandBuffers = null;
            m_commandPool = null;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vkctrl.Dispose();
        }

        private void setImageMemoryBarrier(VkCommandBuffer command,
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
        private VkCommandPool m_commandPool;
        private VkCommandBuffer[] m_commandBuffers;
        private uint m_frameCount;
    }
}
