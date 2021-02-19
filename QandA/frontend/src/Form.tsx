// eslint-disable-next-line @typescript-eslint/no-unused-vars
import React, { FC, useState, createContext, FormEvent } from 'react';
import { PrimaryButton, gray5, gray6 } from './Styles';
/** @jsx jsx */
import { css, jsx } from '@emotion/core';

// indexable type
export interface Values {
  [key: string]: any;
}

export interface Errors {
  [key: string]: string[];
}

/*
We are only going to render a validation error if the field has been
touched and lost focus, so we need to track whether this is the case
for each field:
*/
export interface Touched {
  [key: string]: boolean;
}

// form context custom containing type
interface FormContextProps {
  values: Values;
  setValue?: (fieldName: string, value: any) => void;
  // validation errors handling:
  errors: Errors;
  validate?: (fieldName: string) => void;
  touched: Touched;
  setTouched?: (fieldName: string) => void;
}

// form context
export const FormContext = createContext<FormContextProps>({
  values: {},
  errors: {},
  touched: {},
});

// type alias:
type Validator = (value: any, args?: any) => string;

// checks if something as string is not empty, null or undefined
export const required: Validator = (value: any): string =>
  value === undefined || value === null || value === ''
    ? 'This must be populated'
    : '';

// checks is string have at least 'length' characters
export const minLength: Validator = (value: any, length: number): string =>
  value && value.length < length
    ? `This must be at least ${length} characters`
    : '';

interface Validation {
  validator: Validator;
  arg?: any;
}
interface ValidationProp {
  [key: string]: Validation | Validation[];
}

export interface SubmitResult {
  success: boolean;
  errors?: Errors;
}

interface Props {
  submitCaption?: string;
  validationRules?: ValidationProp;
  onSubmit: (values: Values) => Promise<SubmitResult> | void;
  // result will be passed through the props if the
  // submission is handled via a Redux store
  submitResult?: SubmitResult;
  successMessage?: string;
  failureMessage?: string;
}

export const Form: FC<Props> = ({
  submitCaption,
  children,
  validationRules,
  onSubmit,
  submitResult,
  successMessage = 'Success!',
  failureMessage = 'Something went wrong',
}) => {
  const [values, setValues] = useState<Values>({});
  const [errors, setErrors] = useState<Errors>({});
  const [touched, setTouched] = useState<Touched>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [submitError, setSubmitError] = useState(false);

  const validate = (fieldName: string): string[] => {
    if (!validationRules) {
      return [];
    }
    if (!validationRules[fieldName]) {
      return [];
    }
    const rules = Array.isArray(validationRules[fieldName])
      ? (validationRules[fieldName] as Validation[])
      : ([validationRules[fieldName]] as Validation[]);
    const fieldErrors: string[] = [];
    rules.forEach((rule) => {
      const error = rule.validator(values[fieldName], rule.arg);
      if (error) {
        fieldErrors.push(error);
      }
    });
    const newErrors = { ...errors, [fieldName]: fieldErrors };
    setErrors(newErrors);
    return fieldErrors;
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (validateForm()) {
      // TODO - set state to indicate submission is in progress
      setSubmitting(true);
      setSubmitError(false);
      // TODO - call the consumer submit function
      const result = await onSubmit(values);

      // The result may be passed through as a prop
      if (result === undefined) {
        return;
      }
      // TODO - set any errors in state
      setErrors(result.errors || {});
      setSubmitError(!result.success);
      // TODO - set state to indicate submission has finished
      setSubmitting(false);
      setSubmitted(true);
    }
  };

  const validateForm = () => {
    const newErrors: Errors = {};
    let haveError: boolean = false;
    if (validationRules) {
      Object.keys(validationRules).forEach((fieldName) => {
        newErrors[fieldName] = validate(fieldName);
        if (newErrors[fieldName].length > 0) {
          haveError = true;
        }
      });
    }
    setErrors(newErrors);
    return !haveError;
  };

  const disabled = submitResult
    ? submitResult.success
    : submitting || (submitted && !submitError);
  const showError = submitResult
    ? !submitResult.success
    : submitted && submitError;
  const showSuccess = submitResult
    ? submitResult.success
    : submitted && !submitError;

  return (
    // form context provider injecting state to all children
    <FormContext.Provider
      value={{
        values,
        setValue: (fieldName: string, value: any) => {
          // create new object with all old values + new value
          // this is effectively copy and override part
          // then this is set as new state
          setValues({ ...values, [fieldName]: value });
        },
        errors,
        validate,
        touched,
        setTouched: (fieldName: string) => {
          setTouched({ ...touched, [fieldName]: true });
        },
      }}
    >
      <form noValidate={true} onSubmit={handleSubmit}>
        {/* disable whan submitting or submitted */}
        <fieldset
          disabled={disabled /* submitting || (submitted && !submitError) */}
          css={css`
            margin: 10px auto 0 auto;
            padding: 30px;
            width: 350px;
            background-color: ${gray6};
            border-radius: 4px;
            border: 1px solid ${gray5};
            box-shadow: 0 3px 5px 0 rgba(0, 0, 0, 0.16);
          `}
        >
          {/* form details in children: */}
          {children}
          {/* faint horizontal line at top of submit button */}
          <div
            css={css`
              margin: 30px 0px 0px 0px;
              padding: 20px 0px 0px 0px;
              border-top: 1px solid ${gray5};
            `}
          >
            {/* submit button */}
            <PrimaryButton type="submit">{submitCaption}</PrimaryButton>
          </div>
          {
            /* submitted && submitError */ showError && (
              <p
                css={css`
                  color: red;
                `}
              >
                {failureMessage}
              </p>
            )
          }
          {
            /* submitted && !submitError */ showSuccess && (
              <p
                css={css`
                  color: green;
                `}
              >
                {successMessage}
              </p>
            )
          }
        </fieldset>
      </form>
    </FormContext.Provider>
  );
};
