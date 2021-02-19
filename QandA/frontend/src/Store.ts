import {
  QuestionData,
  getUnansweredQuestions,
  postQuestion,
  PostQuestionData,
} from './QuestionsData';
import {
  Action,
  ActionCreator,
  Dispatch,
  Reducer,
  combineReducers,
  Store,
  createStore,
  applyMiddleware,
} from 'redux';
import thunk, { ThunkAction } from 'redux-thunk';

interface QuestionsState {
  // whether the unanswered questions
  // are being loaded from the server
  readonly loading: boolean;
  // unanswered questions
  readonly unanswered: QuestionData[] | null;
  // result of posting a new question
  readonly postedResult?: QuestionData;
}

export interface AppState {
  readonly questions: QuestionsState;
}

const initialQuestionState: QuestionsState = {
  loading: false,
  unanswered: null,
};

// action interface that will indicate that unanswered
// questions are being fetched from the server
interface GettingUnansweredQuestionsAction
  extends Action<'GettingUnansweredQuestions'> {}

// action for when the unanswered questions have been
// retrieved from the server
export interface GotUnansweredQuestionsAction
  extends Action<'GotUnansweredQuestions'> {
  questions: QuestionData[];
}

// action for when a question has been posted to the server
// and we have the response
export interface PostedQuestionAction extends Action<'PostedQuestion'> {
  result: QuestionData | undefined;
}

type QuestionsActions =
  | GettingUnansweredQuestionsAction
  | GotUnansweredQuestionsAction
  | PostedQuestionAction;

export const getUnansweredQuestionsActionCreator: ActionCreator<ThunkAction<
  Promise<void>, // return type for the inner function
  QuestionData[], // type of data within the last action
  null, // type for the parameter that is passed into the nested function
  GotUnansweredQuestionsAction // type of the last action to be dispatched
>> = () => {
  return async (dispatch: Dispatch) => {
    // dispatch the GettingUnansweredQuestions action
    const gettingUnansweredQuestionsAction: GettingUnansweredQuestionsAction = {
      type: 'GettingUnansweredQuestions',
    };
    dispatch(gettingUnansweredQuestionsAction);

    // get the questions from server
    const questions = await getUnansweredQuestions();

    // dispatch the GotUnansweredQuestions action
    const gotUnansweredQuestionAction: GotUnansweredQuestionsAction = {
      questions,
      type: 'GotUnansweredQuestions',
    };
    dispatch(gotUnansweredQuestionAction);
  };
};

export const postQuestionActionCreator: ActionCreator<ThunkAction<
  Promise<void>,
  QuestionData,
  PostQuestionData,
  PostedQuestionAction
>> = (question: PostQuestionData) => {
  return async (dispatch: Dispatch) => {
    const result = await postQuestion(question);
    const postedQuestionAction: PostedQuestionAction = {
      type: 'PostedQuestion',
      result,
    };
    dispatch(postedQuestionAction);
  };
};

// synchronous action creator
export const clearPostedQuestionActionCreator: ActionCreator<PostedQuestionAction> = () => {
  const postedQuestionAction: PostedQuestionAction = {
    type: 'PostedQuestion',
    result: undefined,
  };
  return postedQuestionAction;
};

const questionsReducer: Reducer<QuestionsState, QuestionsActions> = (
  state = initialQuestionState,
  action,
) => {
  // Handle the different actions and return new state
  switch (action.type) {
    case 'GettingUnansweredQuestions': {
      // return new state
      return {
        ...state,
        unanswered: null,
        loading: true,
      };
    }
    case 'GotUnansweredQuestions': {
      // return new state
      return {
        ...state,
        unanswered: action.questions,
        loading: false,
      };
    }
    case 'PostedQuestion': {
      // return new state
      return {
        ...state,
        unanswered: action.result
          ? (state.unanswered || []).concat(action.result)
          : state.unanswered,
        postedResult: action.result,
      };
    }
    default:
      neverReached(action);
  }
  return state;
};

// The never type is a TypeScript type that represents
// something that would never occur
// and is typically used to specify unreachable areas of code.
const neverReached = (never: never) => {};

// combine all our reducers into a single reducer that returns AppState :
const rootReducer = combineReducers<AppState>({
  questions: questionsReducer,
});

export function configureStore(): Store<AppState> {
  const store = createStore(
    rootReducer, // root reducer
    undefined, // initial state
    applyMiddleware(thunk), // Thunk is used to enable asynchronous actions
  );
  return store;
}
