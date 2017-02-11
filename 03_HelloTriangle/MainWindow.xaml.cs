using System;
using System.Windows;
using System.Runtime.InteropServices;
using SharpVulkan;
using VulkanSharpTutorial.Common;
using GlmSharp;
using System.Linq;

namespace HelloTriangle
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public vec3 Position;
        public vec4 Color;
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            vkctrl.EnableValidation = true;

            // 各イベントを繋ぐ.
            vkctrl.VulkanInitialized += Vkctrl_VulkanInitialized;
            vkctrl.VulkanRendering += Vkctrl_VulkanRendering;
            vkctrl.VulkanClosing += Vkctrl_VulkanClosing;
            vkctrl.VulkanResized += Vkctrl_VulkanResized;
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

            // 頂点入力情報の構築.
            m_vertexInputState = CreateVertexInputState();
            // プリミティブの情報 トライアングルリストでデータが生成されている.
            m_inputAssemblyState = new VkPipelineInputAssemblyStateCreateInfo()
            {
                topology = VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST
            };
            // シェーダーステージ情報の構築.
            m_shaderStages = new VkPipelineShaderStageCreateInfo[2]
            {
                SampleHelpers.CreateShader( device, "resource/simpleVS.spv", VkShaderStageFlagBits.VK_SHADER_STAGE_VERTEX_BIT ),
                SampleHelpers.CreateShader( device, "resource/simpleFS.spv", VkShaderStageFlagBits.VK_SHADER_STAGE_FRAGMENT_BIT ),
            };
            // ラスタライザーステートの構築.
            m_rasterizationState = new VkPipelineRasterizationStateCreateInfo();
            // デプスステンシルステートの構築.
            m_depthStencilState = new VkPipelineDepthStencilStateCreateInfo();
            // カラーブレンドステートの構築.
            m_colorBlendState = new VkPipelineColorBlendStateCreateInfo();
            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState();
            m_colorBlendState.attachments = new[] { colorBlendAttachment };

            // マルチサンプルステートの構築.
            m_multisampleState = new VkPipelineMultisampleStateCreateInfo();

            // パイプラインレイアウトの構築.
            m_pipelineLayout = CreatePipelineLayout(device);

            // ビューポートステートの構築.
            m_viewportState = CreateViewportState();

            // グラフィックスパイプラインを構築.
            m_graphicsPipeline = CreateGraphicsPipeline(device, vkctrl.GetControlRenderPass());

            // 頂点バッファの作成.
            CreateVertexBuffer(device, args.PhysicalDevice);
        }

        private void Vkctrl_VulkanRendering(object sender, SharpVulkanWpf.VulkanEventArgs args)
        {
            var device = args.Device;
            var image = vkctrl.AcquireNextImage();
            var framebuffer = vkctrl.AcquireNextFramebuffer();

            var command = m_commandBuffers[0];
            VulkanAPI.vkBeginCommandBuffer(command);

            VkClearColorValue clearColor = new VkClearColorValue();
            clearColor.valF32.R = 0.125f;
            clearColor.valF32.G = 0.25f;
            clearColor.valF32.B = (float)(0.5f * Math.Sin(m_frameCount * 0.1) + 0.5);
            clearColor.valF32.A = 1.0f;

            var renderArea = new VkRect2D()
            {
                offset = new VkOffset2D(),
                extent = vkctrl.GetCurrentExtent(),
            };

            var renderPassBeginInfo = new VkRenderPassBeginInfo()
            {
                framebuffer = framebuffer,
                renderArea = renderArea,
                renderPass = vkctrl.GetControlRenderPass(),
                pClearValues = new[] { new VkClearValue() { color = clearColor } }
            };

            VulkanAPI.vkCmdBeginRenderPass(command, ref renderPassBeginInfo, VkSubpassContents.VK_SUBPASS_CONTENTS_INLINE);

            VulkanAPI.vkCmdBindPipeline(command, VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, m_graphicsPipeline);
            VulkanAPI.vkCmdBindVertexBuffers(command, 0, 1, new[] { m_vertexBuffer }, new[] { (VkDeviceSize)0 });
            VulkanAPI.vkCmdDraw(command, 3, 1, 0, 0);

            VulkanAPI.vkCmdEndRenderPass( command );
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
            VulkanAPI.vkDestroyPipeline(dev, m_graphicsPipeline); m_graphicsPipeline = null;
            VulkanAPI.vkDestroyPipelineLayout(dev, m_pipelineLayout); m_pipelineLayout = null;

            m_shaderStages.Select(x => x.module).ToList().ForEach(x => VulkanAPI.vkDestroyShaderModule(dev, x));

            VulkanAPI.vkDestroyBuffer(dev, m_vertexBuffer); m_vertexBuffer = null;
            VulkanAPI.vkFreeMemory(dev, m_vertexBufferMemory); m_vertexBufferMemory = null;

            VulkanAPI.vkFreeCommandBuffers(dev, m_commandPool, m_commandBuffers);
            VulkanAPI.vkDestroyCommandPool(dev, m_commandPool);
            m_commandBuffers = null;
            m_commandPool = null;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // コントロール内のリソースを破棄.
            vkctrl.Dispose();
        }

        /// <summary>
        /// リサイズの処理が必要になったときに呼ばれます.
        /// </summary>
        /// <param name="e"></param>
        private void Vkctrl_VulkanResized(SharpVulkanWpf.VulkanSizeChangedEventArgs e)
        {
            var device = e.Device;
            m_viewportState = CreateViewportState();
            if (m_graphicsPipeline != null)
                VulkanAPI.vkDestroyPipeline(device, m_graphicsPipeline);
            var renderPass = vkctrl.GetControlRenderPass();
            m_graphicsPipeline = CreateGraphicsPipeline(device, renderPass);
        }

        /// <summary>
        /// 頂点入力の情報を構築します。
        /// </summary>
        /// <returns></returns>
        private VkPipelineVertexInputStateCreateInfo CreateVertexInputState()
        {
            var attribPosition = new VkVertexInputAttributeDescription()
            {
                location = 0,
                binding = 0,
                offset = 0,
                format = VkFormat.VK_FORMAT_R32G32B32_SFLOAT,
            };
            var attribColor = new VkVertexInputAttributeDescription()
            {
                location = 1,
                binding = 0,
                offset = 12,
                format = VkFormat.VK_FORMAT_R32G32B32A32_SFLOAT,
            };
            var vertexInputBindingDesc = new VkVertexInputBindingDescription()
            {
                binding = 0,
                inputRate = VkVertexInputRate.VK_VERTEX_INPUT_RATE_VERTEX,
                stride = (uint)Marshal.SizeOf<Vertex>(),
            };
            var vertexInputState = new VkPipelineVertexInputStateCreateInfo()
            {
                attributeDescriptions = new[] { attribPosition, attribColor },
                bindingDescriptions = new[] { vertexInputBindingDesc },
            };
            return vertexInputState;
        }

        /// <summary>
        /// パイプラインレイアウトを構築します。
        /// </summary>
        /// <returns></returns>
        private VkPipelineLayout CreatePipelineLayout(VkDevice device)
        {
            var createInfo = new VkPipelineLayoutCreateInfo();
            VkPipelineLayout layout = null;
            VulkanAPI.vkCreatePipelineLayout(device, ref createInfo, out layout);
            return layout;
        }

        private VkPipelineViewportStateCreateInfo CreateViewportState()
        {
            VkExtent2D currentExtent = vkctrl.GetCurrentExtent();
            var viewport = new VkViewport()
            {
                width = currentExtent.width,
                height = currentExtent.height,
                x = 0,
                y = 0,
                minDepth = 0.0f,
                maxDepth = 1.0f,
            };
            var scissor = new VkRect2D()
            {
                extent = currentExtent
            };
            var viewportState = new VkPipelineViewportStateCreateInfo();
            viewportState.viewports = new[] { viewport };
            viewportState.scissors = new[] { scissor };
            return viewportState;
        }

        private VkPipeline CreateGraphicsPipeline(VkDevice device, VkRenderPass renderPass)
        {
            VkPipeline pipeline = null;
            var createInfo = new VkGraphicsPipelineCreateInfo()
            {
                inputAssemblyState = m_inputAssemblyState,
                vertexInputState = m_vertexInputState,
                pRasterizationState = m_rasterizationState,
                pDepthStencilState = m_depthStencilState,
                pColorBlendState = m_colorBlendState,
                pMultisampleState = m_multisampleState,
                pStages = m_shaderStages,
                viewportState = m_viewportState,
                layout = m_pipelineLayout,
                renderPass = renderPass
            };
            VulkanAPI.vkCreateGraphicsPipelines(device, null, 1, ref createInfo, out pipeline);
            return pipeline;
        }

        private void CreateVertexBuffer( VkDevice device, VkPhysicalDevice physicalDevice )
        {
            // 初期頂点データ.
            var vertices = new Vertex[3] {
                new Vertex() { Position = new vec3(-.5f,0.5f,0.0f), Color= new vec4(0.0f, 0.0f, 1.0f, 1.0f) },
                new Vertex() { Position = new vec3(+.5f,0.5f,0.0f), Color= new vec4(0.0f, 1.0f, 0.0f, 1.0f) },
                new Vertex() { Position = new vec3(0.0f,-.5f,0.0f), Color= new vec4(1.0f, 0.0f, 0.0f, 1.0f) },
            };
            var bufferSize = Marshal.SizeOf<Vertex>() * vertices.Length;
            VkMemoryPropertyFlags memoryFlags = VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT | VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_HOST_COHERENT_BIT;
            SampleHelpers.CreateBuffer(device, physicalDevice, bufferSize, VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT, memoryFlags, out m_vertexBuffer, out m_vertexBufferMemory);

            // 初期頂点データの書き込み.
            MappedMemoryStream mappedStream;
            VulkanAPI.vkMapMemory(device, m_vertexBufferMemory, 0, VkDeviceSize.VK_WHOLE_SIZE, 0, out mappedStream);
            mappedStream.Write(vertices);
            VulkanAPI.vkUnmapMemory(device, m_vertexBufferMemory);
        }

        private VkCommandPool m_commandPool;
        private VkCommandBuffer[] m_commandBuffers;
        private uint m_frameCount;

        // 描画に必要になるパイプラインを構築するためのメンバ.
        private VkPipelineVertexInputStateCreateInfo m_vertexInputState;
        private VkPipelineInputAssemblyStateCreateInfo m_inputAssemblyState;
        private VkPipelineShaderStageCreateInfo[] m_shaderStages;
        private VkPipelineRasterizationStateCreateInfo m_rasterizationState;
        private VkPipelineDepthStencilStateCreateInfo m_depthStencilState;
        private VkPipelineColorBlendStateCreateInfo m_colorBlendState;
        private VkPipelineMultisampleStateCreateInfo m_multisampleState;
        private VkPipelineViewportStateCreateInfo m_viewportState;

        private VkPipelineLayout m_pipelineLayout;
        private VkPipeline m_graphicsPipeline;

        // 頂点バッファ関連.
        private VkBuffer m_vertexBuffer;
        private VkDeviceMemory m_vertexBufferMemory;
    }
}
