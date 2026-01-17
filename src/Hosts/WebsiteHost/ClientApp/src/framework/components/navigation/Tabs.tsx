import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';


export interface Tab {
  id: string;
  label: string;
  content: React.ReactNode;
}

export interface TabsProps {
  tabs: Tab[];
  defaultTab?: string;
}

// Creates a set of tabs, that can be used to switch between different views.
// The tabs are wired into the React router using URL hash fragments, so that the active tab is highlighted
// and the URL reflects the current tab (shareable/bookmarkable).
export const Tabs: React.FC<TabsProps> = ({ tabs, defaultTab }) => {
  const location = useLocation();
  const navigate = useNavigate();

  const getInitialTab = (): string => {
    const hash = location.hash.replace('#', '');
    if (hash && tabs.some((tab) => tab.id === hash)) {
      return hash;
    }
    return defaultTab || tabs[0]?.id;
  };

  const [activeTab, setActiveTab] = useState(getInitialTab);

  useEffect(() => {
    const hash = location.hash.replace('#', '');
    if (hash && tabs.some((tab) => tab.id === hash)) {
      setActiveTab(hash);
    }
  }, [location.hash, tabs]);

  const handleTabChange = (tabId: string) => {
    setActiveTab(tabId);
    navigate(`#${tabId}`, { replace: true });
  };

  return (
    <div className="w-full">
      <div className="border-b border-neutral-200 dark:border-neutral-600">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => handleTabChange(tab.id)}
              className={`py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === tab.id
                  ? 'border-brand-primary text-brand-primary dark:text-brand-primary-300'
                  : 'border-transparent text-neutral-500 dark:text-neutral-400 hover:text-neutral-700 dark:hover:text-neutral-300 hover:border-neutral-300 dark:hover:border-neutral-500'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      <div className="mt-6">{tabs.find((tab) => tab.id === activeTab)?.content}</div>
    </div>
  );
};
