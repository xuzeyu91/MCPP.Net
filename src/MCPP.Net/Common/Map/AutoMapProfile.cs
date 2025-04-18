using AutoMapper;

namespace MCPP.Net.Common.Map
{
    public class AutoMapProfile : Profile
    {
        public AutoMapProfile()
        {
            //映射时忽略null值映射，适用于MapTo场景
            //CreateMap<BizCaseInfoEditDTO, PMP_BizCase_Main>().ForAllMembers(opt => opt.Condition((src, dest, sourceMember) => sourceMember != null));
        }
    }
}