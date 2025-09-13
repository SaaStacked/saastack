import React from 'react';
import { ValidationMode } from 'react-hook-form';
import { ActionResult } from '../../actions/Actions.tsx';

export const RequiredFieldsContext = React.createContext<string[]>([]);
export const ActionContext = React.createContext<ActionResult<any, any, any> | undefined>(undefined);
export const FormValidationContext = React.createContext<keyof ValidationMode>('onBlur');
