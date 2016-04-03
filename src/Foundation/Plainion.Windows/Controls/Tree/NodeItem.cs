﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Plainion.Windows.Interactivity.DragDrop;

namespace Plainion.Windows.Controls.Tree
{
    public class NodeItem : TreeViewItem, IDropable, IDragable
    {
        private readonly StateContainer myStateContainer;
        //private bool myShowChildrenCount;
        //private bool myIsChecked;
        private bool myIsInEditMode;

        internal NodeItem( StateContainer stateContainer )
        {
            myStateContainer = stateContainer;

            //ShowChildrenCount = false;

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
        }

        private void OnDataContextChanged( object sender, DependencyPropertyChangedEventArgs e )
        {
            State = myStateContainer.GetOrCreate( DataContext );
            State.Attach( this );
        }

        private void OnLoaded( object sender, RoutedEventArgs e )
        {
            Loaded -= OnLoaded;

            if( BindingOperations.GetBindingExpression( this, FormattedTextProperty ) == null
                && BindingOperations.GetMultiBindingExpression( this, FormattedTextProperty ) == null )
            {
                SetBinding( FormattedTextProperty, new Binding { Path = new PropertyPath( "Text" ), Source = this } );
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new NodeItem( myStateContainer );
        }

        protected override bool IsItemItsOwnContainerOverride( object item )
        {
            return item is NodeItem;
        }

        internal NodeState State { get; private set; }

        public static DependencyProperty TextProperty = DependencyProperty.Register( "Text", typeof( string ), typeof( NodeItem ),
            new FrameworkPropertyMetadata( null ) );

        public string Text
        {
            get { return ( string )GetValue( TextProperty ); }
            set { SetValue( TextProperty, value ); }
        }

        public static DependencyProperty DragAllowedProperty = DependencyProperty.Register( "DragAllowed", typeof( bool ), typeof( NodeItem ),
            new FrameworkPropertyMetadata( true ) );

        public bool DragAllowed
        {
            get { return ( bool )GetValue( DragAllowedProperty ); }
            set { SetValue( DragAllowedProperty, value ); }
        }

        public static DependencyProperty DropAllowedProperty = DependencyProperty.Register( "DropAllowed", typeof( bool ), typeof( NodeItem ),
            new FrameworkPropertyMetadata( true ) );

        public bool DropAllowed
        {
            get { return ( bool )GetValue( DropAllowedProperty ); }
            set { SetValue( DropAllowedProperty, value ); }
        }

        public static DependencyProperty FormattedTextProperty = DependencyProperty.Register( "FormattedText", typeof( string ), typeof( NodeItem ),
            new FrameworkPropertyMetadata( null ) );

        public string FormattedText
        {
            get { return ( string )GetValue( FormattedTextProperty ); }
            set { SetValue( FormattedTextProperty, value ); }
        }

        public bool IsInEditMode
        {
            get { return myIsInEditMode; }
            set
            {
                if( Text == null && value == true )
                {
                    // we first need to set some dummy text so that the EditableTextBlock control becomes visible again
                    Text = "<empty>";
                }

                if( SetProperty( ref myIsInEditMode, value ) )
                {
                    if( !myIsInEditMode && Text == "<empty>" )
                    {
                        Text = null;
                    }
                }
            }
        }

        private bool SetProperty<T>( ref T storage, T value )
        {
            return true;
        }

        public static DependencyProperty IsFilteredOutProperty = DependencyProperty.Register( "IsFilteredOut", typeof( bool ), typeof( TreeViewItem ),
            new FrameworkPropertyMetadata( false, OnIsFilteredOutChanged ) );

        private static void OnIsFilteredOutChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            var self = ( NodeItem )d;
            self.Visibility = self.IsFilteredOut ? Visibility.Collapsed : Visibility.Visible;
        }

        public bool IsFilteredOut
        {
            get { return ( bool )GetValue( IsFilteredOutProperty ); }
            set { SetValue( IsFilteredOutProperty, value ); State.IsFilteredOut = value; }
        }

        private void OnPropertyChanged( [CallerMemberName]string p = null )
        {
        }

        //public bool? IsChecked
        //{
        //    get
        //    {
        //        if( myChildren == null )
        //        {
        //            return myIsChecked;
        //        }

        //        if( Children.All( t => t.IsChecked == true ) )
        //        {
        //            return true;
        //        }

        //        if( Children.All( t => !t.IsChecked == true ) )
        //        {
        //            return false;
        //        }

        //        return null;
        //    }
        //    set
        //    {
        //        if( myChildren == null )
        //        {
        //            myIsChecked = value != null && value.Value;
        //        }
        //        else
        //        {
        //            foreach( var t in Children )
        //            {
        //                t.IsChecked = value.HasValue && value.Value;
        //            }
        //        }

        //        OnPropertyChanged();
        //    }
        //}


        //public bool ShowChildrenCount
        //{
        //    get { return myShowChildrenCount; }
        //    set
        //    {
        //        if (myShowChildrenCount == value)
        //        {
        //            return;
        //        }

        //        myShowChildrenCount = value;

        //        foreach (var child in Children)
        //        {
        //            child.ShowChildrenCount = myShowChildrenCount;
        //        }
        //    }
        //}

        //public string ChildrenCount
        //{
        //    get
        //    {
        //        return ShowChildrenCount && Children.Count > 0
        //            ? string.Format( "[{0}]", Children.Count )
        //            : string.Empty;
        //    }
        //}

        string IDropable.DataFormat
        {
            get { return typeof( NodeItem ).FullName; }
        }

        bool IDropable.IsDropAllowed( object data, DropLocation location )
        {
            if( !( data is NodeItem ) )
            {
                return false;
            }

            if( location == DropLocation.InPlace )
            {
                return DropAllowed;
            }

            // TODO: ask parent
            return true;
        }

        void IDropable.Drop( object data, DropLocation location )
        {
            var droppedElement = data as NodeItem;

            if( droppedElement == null )
            {
                return;
            }

            if( object.ReferenceEquals( droppedElement, this ) )
            {
                //if dragged and dropped yourself, don't need to do anything
                return;
            }

            var arg = new NodeDropRequest
            {
                DroppedNode = droppedElement.State.DataContext,
                DropTarget = State.DataContext,
                Location = location
            };

            var editor = this.FindParentOfType<TreeEditor>();
            if( editor.DropCommand != null && editor.DropCommand.CanExecute( arg ) )
            {
                editor.DropCommand.Execute( arg );
            }

            if( location == DropLocation.InPlace )
            {
                IsExpanded = true;
            }
        }

        Type IDragable.DataType
        {
            get { return DragAllowed ? typeof( NodeItem ) : null; }
        }
    }
}
