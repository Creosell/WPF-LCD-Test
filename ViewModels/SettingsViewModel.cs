using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MvvmHelpers;

namespace WPF_LCD_Test.ViewModels
{
    public class SettingsViewModel : BaseViewModel, IDisposable
    {
        private BaseViewModel _currentPageViewModel; 
        
        public BaseViewModel CurrentPageViewModel 
      
        {
            get => _currentPageViewModel;
            set
            {
                // Опционально: Вызвать метод при уходе со страницы (OnNavigatedFrom), если он есть в PageViewModelBase
                // (_currentPageViewModel as PageViewModelBase)?.OnNavigatedFrom();

                // !!! Важно: очищаем старый ViewModel страницы, если он реализует IDisposable !!!
                (_currentPageViewModel as IDisposable)?.Dispose();

                // Устанавливаем новый ViewModel страницы
                SetProperty(ref _currentPageViewModel, value);

                // Опционально: Вызвать метод при переходе на новую страницу (OnNavigatedTo), если он есть в PageViewModelBase
                // (value as PageViewModelBase)?.OnNavigatedTo();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
