using System.Windows;
using System.Windows.Controls;

namespace PasswordProtector
{
    /// <summary>
    /// 카드 목록 마지막에 표시되는 "새 계정 추가" 카드용 센티넬 객체.
    /// 실제 계정 모델을 오염시키지 않기 위해 단일 인스턴스를 표시 컬렉션에 추가한다.
    /// </summary>
    public sealed class AddAccountPlaceholder
    {
        public static readonly AddAccountPlaceholder Instance = new AddAccountPlaceholder();
        private AddAccountPlaceholder() { }
    }

    /// <summary>
    /// 표시 항목이 일반 계정인지, "새 계정 추가" 카드인지에 따라 템플릿을 선택한다.
    /// </summary>
    public class CardTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? AccountTemplate { get; set; }
        public DataTemplate? AddCardTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is AddAccountPlaceholder)
                return AddCardTemplate;
            return AccountTemplate;
        }
    }
}
