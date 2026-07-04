using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

public class CustomPassRenderFeature : ScriptableRendererFeature
{
	class CustomPass : ScriptableRenderPass
	{
		private LayerMask m_LayerMask;
		private string m_ShaderTagIdName; // 쉐이더에서 찾을 LightMode 이름
		private string m_GlobalTextureName;
		private int m_GlobalTextureID;

		// 렌더링할 때 찾을 쉐이더 태그 리스트
		private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

		public CustomPass(LayerMask layerMask, string shaderTagIdName, string globalTextureName)
		{
			m_LayerMask = layerMask;
			m_ShaderTagIdName = shaderTagIdName;
			m_GlobalTextureName = globalTextureName;
			m_GlobalTextureID = Shader.PropertyToID(globalTextureName);

			// 쉐이더에서 정의한 LightMode 태그를 리스트에 추가 (예: "MyCustomPass")
			m_ShaderTagIdList.Clear();
			m_ShaderTagIdList.Add(new ShaderTagId(m_ShaderTagIdName));
		}

		private class PassData
		{
			public RendererListHandle rendererListHandle;
		}

		private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
		{
			UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			UniversalLightData lightData = frameData.Get<UniversalLightData>();

			var sortFlags = cameraData.defaultOpaqueSortFlags;
			RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
			FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, m_LayerMask);

			// [중요] 여기서 m_ShaderTagIdList를 전달합니다.
			// 렌더러는 이 리스트에 있는 태그(LightMode)를 가진 패스만 찾아서 그립니다.
			// overrideMaterial을 설정하지 않았으므로, 기존 머티리얼의 프로퍼티가 그대로 유지됩니다.
			DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortFlags);

			var param = new RendererListParams(universalRenderingData.cullResults, drawSettings, filterSettings);
			passData.rendererListHandle = renderGraph.CreateRendererList(param);
		}

		static void ExecutePass(PassData data, RasterGraphContext context)
		{
			// 타겟 텍스처를 초록색(예시)으로 클리어
			// 기존 화면(Camera Target)은 건드리지 않습니다.
			context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.green, 1, 0);

			// 리스트 그리기 (각 오브젝트의 머티리얼 설정 유지됨)
			context.cmd.DrawRendererList(data.rendererListHandle);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			string passName = "Custom Pass To Global Texture";

			using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
			{
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

				// 1. 임시 텍스처 생성 (화면 크기와 동일)
				RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
				desc.depthBufferBits = 0; // 깊이는 카메라의 것을 공유하므로 0
				desc.msaaSamples = 1;     // 필요 시 조정

				// TextureDesc 구조체 생성 (Unity 6 API 대응)
				TextureDesc textureDesc = new TextureDesc(desc);
				textureDesc.name = m_GlobalTextureName;
				textureDesc.clearBuffer = false; // ExecutePass에서 Clear하므로 false

				TextureHandle offscreenTexture = renderGraph.CreateTexture(textureDesc);

				// 2. 렌더러 리스트 초기화
				InitRendererLists(frameData, ref passData, renderGraph);

				builder.UseRendererList(passData.rendererListHandle);

				// 3. 타겟 설정
				// Color: 새로 만든 텍스처 (Write)
				// Depth: 기존 카메라의 Depth (Read - 깊이 테스트 수행)
				builder.SetRenderAttachment(offscreenTexture, 0, AccessFlags.Write);
				builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

				// 4. 결과물을 글로벌 텍스처로 등록
				builder.SetGlobalTextureAfterPass(offscreenTexture, m_GlobalTextureID);

				builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
			}
		}
	}

	CustomPass m_ScriptablePass;

	[Header("Settings")]
	public LayerMask m_LayerMask = -1;

	[Tooltip("쉐이더 파일의 Pass 태그 안에 있는 LightMode 이름입니다.")]
	public string m_ShaderPassTag = "MyCustomPass"; // 예: MyCustomPass

	[Tooltip("결과물이 저장될 전역 쉐이더 변수 이름입니다.")]
	public string m_GlobalTextureName = "_MyCustomRenderTexture";

	public override void Create()
	{
		if (string.IsNullOrEmpty(m_GlobalTextureName)) m_GlobalTextureName = "_MyCustomRenderTexture";
		if (string.IsNullOrEmpty(m_ShaderPassTag)) m_ShaderPassTag = "MyCustomPass";

		m_ScriptablePass = new CustomPass(m_LayerMask, m_ShaderPassTag, m_GlobalTextureName);
		m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		renderer.EnqueuePass(m_ScriptablePass);
	}
}